using System.Runtime.InteropServices;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;

namespace HEngine.Rendering.Managers;

public class DirectX12MeshPipelineManager : IDisposable
{
    private readonly D3D12 _d3d12 = D3D12.GetApi();
    private readonly object _rebuildLock = new();
    private bool _disposed;
    private ComPtr<ID3D12PipelineState> _pipelineState;
    private ComPtr<ID3D12RootSignature> _rootSignature;
    private ComPtr<ID3D12Device> _device;

    public ComPtr<ID3D12RootSignature> RootSignature => _rootSignature;
    public ComPtr<ID3D12PipelineState> PipelineState => _pipelineState;

    public void Dispose()
    {
        if (_disposed) return;

        _pipelineState.Dispose();
        _rootSignature.Dispose();
        _d3d12.Dispose();
        _disposed = true;
    }

    public void Initialize(ComPtr<ID3D12Device> device, DirectX12MeshShaderManager shaderManager)
    {
        _device = device;
        CreateRootSignature(device);
        CreatePipelineState(device, shaderManager);
    }

    public void Rebuild(DirectX12MeshShaderManager shaderManager)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(DirectX12MeshPipelineManager));

        lock (_rebuildLock)
        {
            _pipelineState.Dispose();
            CreatePipelineState(_device, shaderManager);
        }
    }

    private void CreateRootSignature(ComPtr<ID3D12Device> device)
    {
        unsafe
        {
                var shadowSrvRange = new DescriptorRange
                {
                    RangeType = DescriptorRangeType.Srv,
                    NumDescriptors = 1,
                    BaseShaderRegister = 5,
                    RegisterSpace = 0,
                    OffsetInDescriptorsFromTableStart = 0
                };

                var materialSrvRange = new DescriptorRange
                {
                    RangeType = DescriptorRangeType.Srv,
                    NumDescriptors = 5,
                    BaseShaderRegister = 0,
                    RegisterSpace = 0,
                    OffsetInDescriptorsFromTableStart = 0
                };

                var shadowSrvRangePtr = &shadowSrvRange;
                var materialSrvRangePtr = &materialSrvRange;
                var rootParameters = new RootParameter[]
                {
                    new()
                    {
                        ParameterType = RootParameterType.TypeCbv,
                        ShaderVisibility = ShaderVisibility.All,
                        Descriptor = new RootDescriptor { ShaderRegister = 0, RegisterSpace = 0 }
                    },
                    new()
                    {
                        ParameterType = RootParameterType.TypeCbv,
                        ShaderVisibility = ShaderVisibility.All,
                        Descriptor = new RootDescriptor { ShaderRegister = 1, RegisterSpace = 0 }
                    },
                    new()
                    {
                        ParameterType = RootParameterType.TypeCbv,
                        ShaderVisibility = ShaderVisibility.All,
                        Descriptor = new RootDescriptor { ShaderRegister = 2, RegisterSpace = 0 }
                    },
                    new()
                    {
                        ParameterType = RootParameterType.TypeDescriptorTable,
                        ShaderVisibility = ShaderVisibility.Pixel,
                        DescriptorTable = new RootDescriptorTable { NumDescriptorRanges = 1, PDescriptorRanges = shadowSrvRangePtr }
                    },
                    new()
                    {
                        ParameterType = RootParameterType.TypeCbv,
                        ShaderVisibility = ShaderVisibility.Pixel,
                        Descriptor = new RootDescriptor { ShaderRegister = 3, RegisterSpace = 0 }
                    },
                    new()
                    {
                        ParameterType = RootParameterType.TypeDescriptorTable,
                        ShaderVisibility = ShaderVisibility.Pixel,
                        DescriptorTable = new RootDescriptorTable { NumDescriptorRanges = 1, PDescriptorRanges = materialSrvRangePtr }
                    }
                };

            var shadowSampler = new StaticSamplerDesc
            {
                Filter = Filter.ComparisonMinMagMipLinear,
                AddressU = Silk.NET.Direct3D12.TextureAddressMode.Clamp,
                AddressV = Silk.NET.Direct3D12.TextureAddressMode.Clamp,
                AddressW = Silk.NET.Direct3D12.TextureAddressMode.Clamp,
                MipLODBias = 0f,
                MaxAnisotropy = 0,
                ComparisonFunc = ComparisonFunc.LessEqual,
                BorderColor = StaticBorderColor.OpaqueWhite,
                MinLOD = 0f,
                MaxLOD = float.MaxValue,
                ShaderRegister = 1,
                RegisterSpace = 0,
                ShaderVisibility = ShaderVisibility.Pixel
            };

            var linearSampler = new StaticSamplerDesc
            {
                Filter = Filter.MinMagMipLinear,
                AddressU = Silk.NET.Direct3D12.TextureAddressMode.Wrap,
                AddressV = Silk.NET.Direct3D12.TextureAddressMode.Wrap,
                AddressW = Silk.NET.Direct3D12.TextureAddressMode.Wrap,
                MipLODBias = 0f,
                MaxAnisotropy = 0,
                ComparisonFunc = ComparisonFunc.Never,
                BorderColor = StaticBorderColor.OpaqueWhite,
                MinLOD = 0f,
                MaxLOD = float.MaxValue,
                ShaderRegister = 0,
                RegisterSpace = 0,
                ShaderVisibility = ShaderVisibility.Pixel
            };

            var staticSamplers = new[] { shadowSampler, linearSampler };
            fixed (RootParameter* rootParametersPtr = rootParameters)
            fixed (StaticSamplerDesc* staticSamplersPtr = staticSamplers)
            {
            var rootSignatureDesc = new RootSignatureDesc
            {
                NumParameters = (uint)rootParameters.Length,
                PParameters = rootParametersPtr,
                NumStaticSamplers = (uint)staticSamplers.Length,
                PStaticSamplers = staticSamplersPtr,
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
    }

    private void CreatePipelineState(ComPtr<ID3D12Device> device, DirectX12MeshShaderManager shaderManager)
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
                }
            };

            fixed (InputElementDesc* inputElementsPtr = inputElements)
            {
                var psoDesc = new GraphicsPipelineStateDesc
                {
                    InputLayout = new InputLayoutDesc
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
                        CullMode = CullMode.Back,
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
                        DepthEnable = 1,
                        DepthWriteMask = DepthWriteMask.All,
                        DepthFunc = ComparisonFunc.Less,
                        StencilEnable = 0,
                        StencilReadMask = 0,
                        StencilWriteMask = 0
                    },
                    SampleMask = uint.MaxValue,
                    PrimitiveTopologyType = PrimitiveTopologyType.Triangle,
                    NumRenderTargets = 1,
                    SampleDesc = new SampleDesc { Count = 1, Quality = 0 },
                    DSVFormat = Format.FormatD32Float
                };

                psoDesc.RTVFormats[0] = Format.FormatR8G8B8A8Unorm;
                psoDesc.BlendState.RenderTarget[0] = new RenderTargetBlendDesc
                {
                    BlendEnable = 0,
                    RenderTargetWriteMask = (byte)ColorWriteEnable.All
                };

                var result = device.CreateGraphicsPipelineState(in psoDesc, out _pipelineState);
                if (result < 0)
                    throw new Exception($"Failed to create mesh pipeline state. HRESULT: {result:X8}");
            }

            foreach (var element in inputElements)
                Marshal.FreeHGlobal((nint)element.SemanticName);
        }
    }
}