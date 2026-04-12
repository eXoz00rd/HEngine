using System.Runtime.InteropServices;
using System.Text;
using HEngine.Core.Configuration;
using Microsoft.Extensions.Logging;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D.Compilers;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;

namespace HEngine.Rendering.Managers;

/// <summary>
/// Manages the pipeline state object (PSO) and root signature used for depth-only shadow passes.
/// </summary>
public sealed class ShadowPipelineStateManager : IDisposable
{
    private readonly D3D12 _d3d12 = D3D12.GetApi();
    private readonly D3DCompiler _compiler = D3DCompiler.GetApi();
    private readonly ShaderFileLoader _fileLoader;
    private readonly ILogger<ShadowPipelineStateManager>? _logger;

    private ComPtr<ID3D12Device> _device;
    private ComPtr<ID3D12RootSignature> _rootSignature;
    private ComPtr<ID3D12PipelineState> _pipelineState;
    private ComPtr<ID3D10Blob> _vertexShader;

    private bool _initialized;
    private bool _disposed;

    public ComPtr<ID3D12RootSignature> RootSignature => _rootSignature;
    public ComPtr<ID3D12PipelineState> PipelineState => _pipelineState;
    public bool IsInitialized => _initialized;

    public ShadowPipelineStateManager(
        ShaderFileLoader fileLoader,
        ILogger<ShadowPipelineStateManager>? logger = null)
    {
        _fileLoader = fileLoader;
        _logger = logger;
    }

    public void Initialize(ComPtr<ID3D12Device> device, ShadowSettings settings)
    {
        if (_initialized) Dispose();

        _device = device;
        _vertexShader = CompileShadowVS();
        CreateRootSignature();
        CreatePipelineState(settings);

        _initialized = true;
        _logger?.LogDebug("ShadowPipelineStateManager initialized.");
    }

    public void Rebuild(ShadowSettings settings)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ShadowPipelineStateManager));
        _pipelineState.Dispose();
        CreatePipelineState(settings);
    }

    private unsafe void CreateRootSignature()
    {
        var constants = new RootParameter
        {
            ParameterType = RootParameterType.Type32BitConstants,
            ShaderVisibility = ShaderVisibility.Vertex,
            Constants = new RootConstants
            {
                ShaderRegister = 0,
                RegisterSpace = 0,
                Num32BitValues = 32
            }
        };

        var desc = new RootSignatureDesc
        {
            NumParameters = 1,
            PParameters = &constants,
            Flags = RootSignatureFlags.AllowInputAssemblerInputLayout
        };

        ID3D10Blob* sig = null;
        ID3D10Blob* err = null;

        var hr = _d3d12.SerializeRootSignature(
            in desc, D3DRootSignatureVersion.Version1, ref sig, ref err);

        if (hr < 0)
        {
            string? msg = null;
            if (err != null)
            {
                msg = Marshal.PtrToStringAnsi((nint)err->GetBufferPointer());
                err->Release();
            }
            throw new InvalidOperationException(
                $"Shadow root signature serialization failed: {msg} (HRESULT {hr:X8})");
        }

        var sigBlob = new ComPtr<ID3D10Blob>(sig);
        hr = _device.CreateRootSignature(
            0, sigBlob.GetBufferPointer(), sigBlob.GetBufferSize(), out _rootSignature);
        sigBlob.Dispose();

        if (hr < 0)
            throw new InvalidOperationException($"Shadow root signature creation failed. HRESULT: {hr:X8}");
    }

    private unsafe void CreatePipelineState(ShadowSettings settings)
    {
        var inputElements = new[]
        {
            new InputElementDesc
            {
                SemanticName = (byte*)Marshal.StringToHGlobalAnsi("POSITION"),
                SemanticIndex = 0,
                Format = Format.FormatR32G32B32Float,
                InputSlot = 0,
                AlignedByteOffset = 0,
                InputSlotClass = InputClassification.PerVertexData,
                InstanceDataStepRate = 0
            },
            new InputElementDesc
            {
                SemanticName = (byte*)Marshal.StringToHGlobalAnsi("NORMAL"),
                SemanticIndex = 0,
                Format = Format.FormatR32G32B32Float,
                InputSlot = 0,
                AlignedByteOffset = 12,
                InputSlotClass = InputClassification.PerVertexData,
                InstanceDataStepRate = 0
            },
            new InputElementDesc
            {
                SemanticName = (byte*)Marshal.StringToHGlobalAnsi("TEXCOORD"),
                SemanticIndex = 0,
                Format = Format.FormatR32G32Float,
                InputSlot = 0,
                AlignedByteOffset = 24,
                InputSlotClass = InputClassification.PerVertexData,
                InstanceDataStepRate = 0
            },
            new InputElementDesc
            {
                SemanticName = (byte*)Marshal.StringToHGlobalAnsi("COLOR"),
                SemanticIndex = 0,
                Format = Format.FormatR32G32B32A32Float,
                InputSlot = 0,
                AlignedByteOffset = 32,
                InputSlotClass = InputClassification.PerVertexData,
                InstanceDataStepRate = 0
            },
        };

        fixed (InputElementDesc* inputPtr = inputElements)
        {
            var psoDesc = new GraphicsPipelineStateDesc
            {
                PRootSignature = _rootSignature,
                InputLayout = new InputLayoutDesc
                {
                    PInputElementDescs = inputPtr,
                    NumElements = (uint)inputElements.Length
                },
                VS = new ShaderBytecode
                {
                    PShaderBytecode = _vertexShader.GetBufferPointer(),
                    BytecodeLength = _vertexShader.GetBufferSize()
                },
                RasterizerState = new RasterizerDesc
                {
                    FillMode = FillMode.Solid,
                    CullMode = CullMode.Front,
                    FrontCounterClockwise = 0,
                    DepthBias = (int)(settings.DepthBias * 1000f),
                    DepthBiasClamp = 0.0f,
                    SlopeScaledDepthBias = settings.SlopeScaledDepthBias,
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
                    DepthEnable = 1,
                    DepthWriteMask = DepthWriteMask.All,
                    DepthFunc = ComparisonFunc.Less,
                    StencilEnable = 0
                },
                SampleMask = uint.MaxValue,
                PrimitiveTopologyType = PrimitiveTopologyType.Triangle,
                NumRenderTargets = 0,
                DSVFormat = Format.FormatD32Float,
                SampleDesc = new SampleDesc { Count = 1, Quality = 0 }
            };

            var hr = _device.CreateGraphicsPipelineState(in psoDesc, out _pipelineState);
            if (hr < 0)
                throw new InvalidOperationException(
                    $"Shadow PSO creation failed. HRESULT: {hr:X8}");
        }

        foreach (var element in inputElements)
            Marshal.FreeHGlobal((nint)element.SemanticName);
    }

    private ComPtr<ID3D10Blob> CompileShadowVS()
    {
        var shaderCode = _fileLoader.LoadShaderCode("ShadowPass.hlsl");
        return CompileShader(shaderCode, "VSDepthOnly", "vs_5_0");
    }

    private unsafe ComPtr<ID3D10Blob> CompileShader(string code, string entryPoint, string target)
    {
        var codeBytes = Encoding.UTF8.GetBytes(code);
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
                    $"Shadow VS compilation failed (entry={entryPoint}): {msg}");
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
        _compiler.Dispose();
        _d3d12.Dispose();

        _initialized = false;
        _disposed = true;
    }
}

