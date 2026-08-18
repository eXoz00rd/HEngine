using System.Runtime.InteropServices;
using System.Text;
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
    private readonly D3DCompiler _compiler = D3DCompiler.GetApi();
    private readonly ShaderFileLoader _fileLoader;
    private readonly ILogger<DirectX12PostProcessPipelineManager>? _logger;

    private ComPtr<ID3D12Device> _device;
    private ComPtr<ID3D12RootSignature> _rootSignature;
    private ComPtr<ID3D12PipelineState> _pipelineState;
    private ComPtr<ID3D10Blob> _vertexShader;
    private ComPtr<ID3D10Blob> _pixelShader;

    private bool _initialized;
    private bool _disposed;

    public ComPtr<ID3D12RootSignature> RootSignature => _rootSignature;
    public ComPtr<ID3D12PipelineState> PipelineState => _pipelineState;
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

    public void Initialize(ComPtr<ID3D12Device> device, Format renderTargetFormat)
    {
        if (_initialized) Dispose();

        _device = device;
        _vertexShader = CompileShader("VSMain", "vs_5_0");
        _pixelShader = CompileShader("PSMain", "ps_5_0");
        CreateRootSignature();
        CreatePipelineState(renderTargetFormat);

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

            var sigBlob = new ComPtr<ID3D10Blob>(sig);
            hr = _device.CreateRootSignature(
                0, sigBlob.GetBufferPointer(), sigBlob.GetBufferSize(), out _rootSignature);
            sigBlob.Dispose();

            if (hr < 0)
                throw new InvalidOperationException($"PostProcess root signature creation failed. HRESULT: {hr:X8}");
        }
    }

    private unsafe void CreatePipelineState(Format renderTargetFormat)
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
                PShaderBytecode = _vertexShader.GetBufferPointer(),
                BytecodeLength = _vertexShader.GetBufferSize()
            },
            PS = new ShaderBytecode
            {
                PShaderBytecode = _pixelShader.GetBufferPointer(),
                BytecodeLength = _pixelShader.GetBufferSize()
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

        var hr = _device.CreateGraphicsPipelineState(in psoDesc, out _pipelineState);
        if (hr < 0)
            throw new InvalidOperationException($"PostProcess PSO creation failed. HRESULT: {hr:X8}");
    }

    private unsafe ComPtr<ID3D10Blob> CompileShader(string entryPoint, string target)
    {
        var shaderCode = _fileLoader.LoadShaderCode("ToneMapping.hlsl");
        var codeBytes = Encoding.UTF8.GetBytes(shaderCode);
        var entryBytes = Encoding.UTF8.GetBytes(entryPoint);
        var targetBytes = Encoding.UTF8.GetBytes(target);

        fixed (byte* codePtr = codeBytes)
        fixed (byte* entryPtr = entryBytes)
        fixed (byte* targetPtr = targetBytes)
        {
            ID3D10Blob* blob = null;
            ID3D10Blob* error = null;

            var hr = _compiler.Compile(
                codePtr, (nuint)codeBytes.Length,
                (byte*)null, null, null,
                entryPtr, targetPtr,
                0u, 0u, ref blob, ref error);

            if (hr < 0)
            {
                string? msg = null;
                if (error != null)
                {
                    msg = Marshal.PtrToStringAnsi((nint)error->GetBufferPointer(), (int)error->GetBufferSize());
                    error->Release();
                }

                throw new InvalidOperationException(
                    $"PostProcess shader compilation failed (entry={entryPoint}): {msg}");
            }

            if (error != null) error->Release();
            return new ComPtr<ID3D10Blob>(blob);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _pipelineState.Dispose();
        _rootSignature.Dispose();
        _vertexShader.Dispose();
        _pixelShader.Dispose();
        _compiler.Dispose();
        _d3d12.Dispose();

        _initialized = false;
        _disposed = true;
    }
}
