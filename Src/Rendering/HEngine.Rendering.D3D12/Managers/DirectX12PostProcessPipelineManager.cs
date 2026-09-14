using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D.Compilers;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;

namespace HEngine.Rendering.Managers;

/// <summary>
/// Manages the root signature and pipeline state object (PSO) for the ToneMapping
/// fullscreen post-process pass: a root-constants cbuffer (b0), a source-texture SRV
/// descriptor table (t0) and a static linear-clamp sampler (s0).
/// </summary>
public sealed class DirectX12PostProcessPipelineManager : IDisposable
{
    private readonly D3D12 _d3d12 = D3D12.GetApi();
    private readonly ShaderVariantCompiler _shaderCompiler = new();
    private readonly ShaderFileLoader _fileLoader;
    private readonly ILogger<DirectX12PostProcessPipelineManager>? _logger;

    private ComPtr<ID3D12Device> _device;
    private ComPtr<ID3D12RootSignature> _rootSignature;
    private ComPtr<ID3D12PipelineState> _pipelineState;
    private ComPtr<ID3D10Blob> _vertexShader;
    private ComPtr<ID3D10Blob> _pixelShader;

    private ComPtr<ID3D12PipelineState> _backBufferPipelineState;
    private ComPtr<ID3D10Blob> _blitVertexShader;
    private ComPtr<ID3D10Blob> _blitPixelShader;

    private bool _initialized;
    private bool _disposed;

    public ComPtr<ID3D12RootSignature> RootSignature => _rootSignature;
    public ComPtr<ID3D12PipelineState> PipelineState => _pipelineState;

    /// <summary>
    /// Tonemap-free passthrough PSO (<c>PostProcessBlit.hlsl</c>) targeting the swap chain's back-buffer
    /// format. Used for the final resolve of the post-process chain's output into the back buffer, since
    /// reusing <see cref="PipelineState"/> there would apply ToneMapping's exposure/gamma a second time
    /// (tracks #45).
    /// </summary>
    public ComPtr<ID3D12PipelineState> BackBufferPipelineState => _backBufferPipelineState;

    public bool IsInitialized => _initialized;

    public const uint SourceTextureRootParameterIndex = 1;
    public const uint ConstantsRootParameterIndex = 0;

    public DirectX12PostProcessPipelineManager(
        ShaderFileLoader fileLoader,
        ILogger<DirectX12PostProcessPipelineManager>? logger = null)
    {
        _fileLoader = fileLoader ?? throw new ArgumentNullException(nameof(fileLoader));
        _logger = logger;
    }

    public void Initialize(ComPtr<ID3D12Device> device, Format renderTargetFormat, Format backBufferFormat)
    {
        if (_initialized) Dispose();
        _disposed = false;

        _device = device;
        _vertexShader = CompileShader("ToneMapping.hlsl", "VSMain", "vs_5_0");
        _pixelShader = CompileShader("ToneMapping.hlsl", "PSMain", "ps_5_0");
        CreateRootSignature();
        _pipelineState = CreatePipelineState(_vertexShader, _pixelShader, renderTargetFormat);

        _blitVertexShader = CompileShader("PostProcessBlit.hlsl", "VSMain", "vs_5_0");
        _blitPixelShader = CompileShader("PostProcessBlit.hlsl", "PSMain", "ps_5_0");
        _backBufferPipelineState = CreatePipelineState(_blitVertexShader, _blitPixelShader, backBufferFormat);

        _initialized = true;
        _logger?.LogDebug("DirectX12PostProcessPipelineManager initialized.");
    }

    private unsafe void CreateRootSignature()
    {
        var descriptorRange = new DescriptorRange
        {
            RangeType = DescriptorRangeType.Srv,
            NumDescriptors = 1,
            BaseShaderRegister = 0,
            RegisterSpace = 0,
            OffsetInDescriptorsFromTableStart = 0
        };

        var rootParameters = new RootParameter[]
        {
            new()
            {
                ParameterType = RootParameterType.Type32BitConstants,
                ShaderVisibility = ShaderVisibility.Pixel,
                Constants = new RootConstants
                {
                    ShaderRegister = 0,
                    RegisterSpace = 0,
                    Num32BitValues = 4
                }
            },
            new()
            {
                ParameterType = RootParameterType.TypeDescriptorTable,
                ShaderVisibility = ShaderVisibility.Pixel,
                DescriptorTable = new RootDescriptorTable
                {
                    NumDescriptorRanges = 1,
                    PDescriptorRanges = &descriptorRange
                }
            }
        };

        var staticSampler = new StaticSamplerDesc
        {
            Filter = Filter.MinMagMipLinear,
            AddressU = Silk.NET.Direct3D12.TextureAddressMode.Clamp,
            AddressV = Silk.NET.Direct3D12.TextureAddressMode.Clamp,
            AddressW = Silk.NET.Direct3D12.TextureAddressMode.Clamp,
            MipLODBias = 0f,
            MaxAnisotropy = 0,
            ComparisonFunc = ComparisonFunc.Never,
            BorderColor = StaticBorderColor.OpaqueBlack,
            MinLOD = 0f,
            MaxLOD = float.MaxValue,
            ShaderRegister = 0,
            RegisterSpace = 0,
            ShaderVisibility = ShaderVisibility.Pixel
        };

        fixed (RootParameter* rootParametersPtr = rootParameters)
        {
            var rootSignatureDesc = new RootSignatureDesc
            {
                NumParameters = (uint)rootParameters.Length,
                PParameters = rootParametersPtr,
                NumStaticSamplers = 1,
                PStaticSamplers = &staticSampler,
                Flags = RootSignatureFlags.None
            };

            ID3D10Blob* sig = null;
            ID3D10Blob* err = null;

            var hr = _d3d12.SerializeRootSignature(
                in rootSignatureDesc, D3DRootSignatureVersion.Version1, ref sig, ref err);

            if (hr < 0)
            {
                string? msg = null;
                if (err != null)
                {
                    msg = Marshal.PtrToStringAnsi((nint)err->GetBufferPointer());
                    err->Release();
                }

                throw new InvalidOperationException(
                    $"PostProcess root signature serialization failed: {msg} (HRESULT {hr:X8})");
            }

            if (err != null) err->Release();

            var sigBlob = new ComPtr<ID3D10Blob>(sig);
            hr = _device.CreateRootSignature(
                0, sigBlob.GetBufferPointer(), sigBlob.GetBufferSize(), out _rootSignature);
            sigBlob.Dispose();

            if (hr < 0)
                throw new InvalidOperationException($"PostProcess root signature creation failed. HRESULT: {hr:X8}");
        }
    }

    private unsafe ComPtr<ID3D12PipelineState> CreatePipelineState(
        ComPtr<ID3D10Blob> vertexShader, ComPtr<ID3D10Blob> pixelShader, Format renderTargetFormat)
    {
        var psoDesc = new GraphicsPipelineStateDesc
        {
            PRootSignature = _rootSignature,
            InputLayout = new InputLayoutDesc
            {
                PInputElementDescs = null,
                NumElements = 0
            },
            VS = new ShaderBytecode
            {
                PShaderBytecode = vertexShader.GetBufferPointer(),
                BytecodeLength = vertexShader.GetBufferSize()
            },
            PS = new ShaderBytecode
            {
                PShaderBytecode = pixelShader.GetBufferPointer(),
                BytecodeLength = pixelShader.GetBufferSize()
            },
            RasterizerState = new RasterizerDesc
            {
                FillMode = FillMode.Solid,
                CullMode = CullMode.None,
                FrontCounterClockwise = 0,
                DepthBias = 0,
                DepthBiasClamp = 0.0f,
                SlopeScaledDepthBias = 0.0f,
                DepthClipEnable = 1,
                MultisampleEnable = 0,
                AntialiasedLineEnable = 0,
                ForcedSampleCount = 0,
                ConservativeRaster = ConservativeRasterizationMode.Off
            },
            BlendState = new BlendDesc
            {
                AlphaToCoverageEnable = 0,
                IndependentBlendEnable = 0
            },
            DepthStencilState = new DepthStencilDesc
            {
                DepthEnable = 0,
                StencilEnable = 0
            },
            SampleMask = uint.MaxValue,
            PrimitiveTopologyType = PrimitiveTopologyType.Triangle,
            NumRenderTargets = 1,
            DSVFormat = Format.FormatUnknown,
            SampleDesc = new SampleDesc { Count = 1, Quality = 0 }
        };

        psoDesc.RTVFormats[0] = renderTargetFormat;
        psoDesc.BlendState.RenderTarget[0] = new RenderTargetBlendDesc
        {
            BlendEnable = 0,
            RenderTargetWriteMask = (byte)ColorWriteEnable.All
        };

        var hr = _device.CreateGraphicsPipelineState(in psoDesc, out ComPtr<ID3D12PipelineState> pipelineState);
        if (hr < 0)
            throw new InvalidOperationException($"PostProcess PSO creation failed. HRESULT: {hr:X8}");

        return pipelineState;
    }

    private ComPtr<ID3D10Blob> CompileShader(string shaderFileName, string entryPoint, string target)
    {
        var shaderCode = _fileLoader.LoadShaderCode(shaderFileName);
        return _shaderCompiler.CompileShader(shaderCode, entryPoint, target, shaderFileName);
    }

    public void Dispose()
    {
        if (_disposed) return;

        _pipelineState.Dispose();
        _backBufferPipelineState.Dispose();
        _rootSignature.Dispose();
        _vertexShader.Dispose();
        _pixelShader.Dispose();
        _blitVertexShader.Dispose();
        _blitPixelShader.Dispose();
        _shaderCompiler.Dispose();
        _d3d12.Dispose();

        _initialized = false;
        _disposed = true;
    }
}
