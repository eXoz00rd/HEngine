using System.Runtime.InteropServices;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;

namespace HEngine.Rendering.Managers;

public class DirectX12PipelineStateManager : IDisposable
{
    private readonly D3D12 _d3d12 = D3D12.GetApi();
    private bool _disposed;
    private ComPtr<ID3D12PipelineState> _pipelineState;
    private ComPtr<ID3D12RootSignature> _rootSignature;

    public ComPtr<ID3D12RootSignature> RootSignature => _rootSignature;
    public ComPtr<ID3D12PipelineState> PipelineState => _pipelineState;

    public void Dispose()
    {
        if (_disposed) return;

        _pipelineState.Dispose();
        _rootSignature.Dispose();
        _disposed = true;
    }

    public void Initialize(ComPtr<ID3D12Device> device, DirectX12ShaderManager shaderManager)
    {
        CreateRootSignature(device);
        CreatePipelineState(device, shaderManager);
    }

    private void CreateRootSignature(ComPtr<ID3D12Device> device)
    {
        unsafe
        {
            var rootParameter = new RootParameter
            {
                ParameterType = RootParameterType.TypeCbv, // Poprawiona nazwa
                ShaderVisibility = ShaderVisibility.Vertex,
                Descriptor = new RootDescriptor
                {
                    ShaderRegister = 0,
                    RegisterSpace = 0
                }
            };

            var rootSignatureDesc = new RootSignatureDesc
            {
                NumParameters = 1,
                PParameters = &rootParameter,
                NumStaticSamplers = 0,
                PStaticSamplers = null,
                Flags = RootSignatureFlags.AllowInputAssemblerInputLayout
            };

            ID3D10Blob* sigPtr = null;
            ID3D10Blob* errPtr = null;

            var result = _d3d12.SerializeRootSignature(
                in rootSignatureDesc,
                D3DRootSignatureVersion.Version1,
                ref sigPtr,
                ref errPtr);

            if (result < 0)
            {
                if (errPtr != null)
                {
                    var errorMessage = Marshal.PtrToStringAnsi((nint)errPtr->GetBufferPointer());
                    Console.WriteLine($"Root signature serialization error: {errorMessage}");
                    errPtr->Release();
                }

                throw new Exception($"Failed to serialize root signature. HRESULT: {result:X8}");
            }

            var signature = new ComPtr<ID3D10Blob>(sigPtr);
            if (errPtr != null)
            {
                var error = new ComPtr<ID3D10Blob>(errPtr);
                error.Dispose();
            }

            result = device.CreateRootSignature(
                0,
                signature.GetBufferPointer(),
                signature.GetBufferSize(),
                out _rootSignature);

            if (result < 0)
                throw new Exception($"Failed to create root signature. HRESULT: {result:X8}");

            signature.Dispose();
        }
    }

    private void CreatePipelineState(ComPtr<ID3D12Device> device, DirectX12ShaderManager shaderManager)
    {
        unsafe
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
                    SemanticName = (byte*)Marshal.StringToHGlobalAnsi("COLOR"),
                    SemanticIndex = 0,
                    Format = Format.FormatR32G32B32A32Float,
                    InputSlot = 0,
                    AlignedByteOffset = 12,
                    InputSlotClass = InputClassification.PerVertexData,
                    InstanceDataStepRate = 0
                }
            };

            fixed (InputElementDesc* inputElementsPtr = inputElements)
            {
                var psoDesc = new GraphicsPipelineStateDesc
                {
                    InputLayout = new InputLayoutDesc // Poprawiona nazwa
                    {
                        PInputElementDescs = inputElementsPtr,
                        NumElements = (uint)inputElements.Length
                    },
                    PRootSignature = _rootSignature,
                    VS = new ShaderBytecode
                    {
                        PShaderBytecode = shaderManager.VertexShader.GetBufferPointer(),
                        BytecodeLength = shaderManager.VertexShader.GetBufferSize()
                    },
                    PS = new ShaderBytecode
                    {
                        PShaderBytecode = shaderManager.PixelShader.GetBufferPointer(),
                        BytecodeLength = shaderManager.PixelShader.GetBufferSize()
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
                    SampleDesc = new SampleDesc { Count = 1, Quality = 0 }
                };

                psoDesc.RTVFormats[0] = Format.FormatR8G8B8A8Unorm;
                psoDesc.BlendState.RenderTarget[0] = new RenderTargetBlendDesc
                {
                    BlendEnable = 0,
                    RenderTargetWriteMask = (byte)ColorWriteEnable.All
                };

                var result = device.CreateGraphicsPipelineState(in psoDesc, out _pipelineState);
                if (result < 0)
                    throw new Exception($"Failed to create pipeline state. HRESULT: {result:X8}");
            }

            // Cleanup
            foreach (var element in inputElements)
                Marshal.FreeHGlobal((nint)element.SemanticName);
        }
    }
}