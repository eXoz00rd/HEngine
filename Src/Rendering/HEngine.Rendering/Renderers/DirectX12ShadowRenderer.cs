using System.Numerics;
using System.Runtime.InteropServices;
using HEngine.Core.Configuration;
using HEngine.Core.Rendering.Contracts;
using HEngine.Rendering.Data;
using HEngine.Rendering.DirectX12;
using HEngine.Rendering.Devices;
using HEngine.Rendering.Managers;
using Microsoft.Extensions.Logging;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using Silk.NET.Maths;
using Range = Silk.NET.Direct3D12.Range;

namespace HEngine.Rendering.Renderers;

public sealed class DirectX12ShadowRenderer : IShadowRenderer, IDisposable
{
    private const int MaxVertices = 65536;
    private const int MaxIndices = 65536 * 3;
    private const int FloatsPerVertex = 12;

    private readonly D3D12 _d3d12 = D3D12.GetApi();
    private readonly IGraphicsDevice _device;
    private readonly ShadowMapManager _shadowMapManager;
    private readonly ShadowPipelineStateManager _pipelineStateManager;
    private readonly ShadowSettings _settings;
    private readonly ILogger<DirectX12ShadowRenderer>? _logger;

    private ComPtr<ID3D12Resource> _vertexBuffer;
    private ComPtr<ID3D12Resource> _indexBuffer;
    private DirectX12CommandQueue? _commandQueue;
    private Matrix4x4 _currentLightVP;
    private bool _gpuInitialized;
    private bool _arrayIsShaderVisible;
    private bool _disposed;

    public DirectX12ShadowRenderer(
        IGraphicsDevice device,
        ShadowMapManager shadowMapManager,
        ShadowPipelineStateManager pipelineStateManager,
        ShadowSettings settings,
        ILogger<DirectX12ShadowRenderer>? logger = null)
    {
        _device = device ?? throw new ArgumentNullException(nameof(device));
        _shadowMapManager = shadowMapManager ?? throw new ArgumentNullException(nameof(shadowMapManager));
        _pipelineStateManager = pipelineStateManager ?? throw new ArgumentNullException(nameof(pipelineStateManager));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger;
    }

    public void BeginShadowPass(int cascadeIndex, Matrix4x4 lightVP, int resolution)
    {
        EnsureGpuResources();

        _currentLightVP = lightVP;

        var commandList = _commandQueue!.CommandList;

        if (_arrayIsShaderVisible)
        {
            TransitionShadowArray(commandList, ResourceStates.PixelShaderResource, ResourceStates.DepthWrite);
            _arrayIsShaderVisible = false;
        }

        var dsvHandle = _shadowMapManager.GetDsvHandle(cascadeIndex);

        unsafe
        {
            commandList.OMSetRenderTargets(0, (CpuDescriptorHandle*)null, false, &dsvHandle);
            commandList.ClearDepthStencilView(dsvHandle, ClearFlags.Depth, 1.0f, 0, 0, (Box2D<int>*)null);
        }

        var viewport = new Viewport
        {
            TopLeftX = 0,
            TopLeftY = 0,
            Width = resolution,
            Height = resolution,
            MinDepth = 0.0f,
            MaxDepth = 1.0f
        };
        commandList.RSSetViewports(1, ref viewport);

        var scissorRect = new Box2D<int>(0, 0, resolution, resolution);
        commandList.RSSetScissorRects(1, in scissorRect);

        commandList.SetPipelineState(_pipelineStateManager.PipelineState);
        commandList.SetGraphicsRootSignature(_pipelineStateManager.RootSignature);
    }

    public void RenderDepthOnlyMesh(Matrix4x4 transform, ReadOnlySpan<float> vertices, ReadOnlySpan<uint> indices)
    {
        if (!_gpuInitialized || _commandQueue is null) return;
        if (vertices.Length == 0 || indices.Length == 0) return;
        if (vertices.Length % FloatsPerVertex != 0) return;

        var vertexCount = vertices.Length / FloatsPerVertex;
        if (vertexCount > MaxVertices || indices.Length > MaxIndices)
        {
            if (_logger?.IsEnabled(LogLevel.Warning) == true)
            {
                _logger.LogWarning(
                    "Shadow caster mesh exceeds shadow buffer capacity ({VertexCount} vertices, {IndexCount} indices); skipping draw",
                    vertexCount, indices.Length);
            }

            return;
        }

        UploadVertexBuffer(vertices);
        UploadIndexBuffer(indices);

        var commandList = _commandQueue.CommandList;

        Span<float> rootConstants = stackalloc float[32];
        WriteMatrix(rootConstants, 0, _currentLightVP);
        WriteMatrix(rootConstants, 16, transform);

        unsafe
        {
            fixed (float* constantsPtr = rootConstants)
            {
                commandList.SetGraphicsRoot32BitConstants(0, 32, constantsPtr, 0);
            }
        }

        commandList.IASetPrimitiveTopology(D3DPrimitiveTopology.D3DPrimitiveTopologyTrianglelist);

        var vertexBufferView = new VertexBufferView
        {
            BufferLocation = _vertexBuffer.GetGPUVirtualAddress(),
            StrideInBytes = FloatsPerVertex * sizeof(float),
            SizeInBytes = (uint)vertices.Length * sizeof(float)
        };
        commandList.IASetVertexBuffers(0, 1, ref vertexBufferView);

        var indexBufferView = new IndexBufferView
        {
            BufferLocation = _indexBuffer.GetGPUVirtualAddress(),
            SizeInBytes = (uint)indices.Length * sizeof(uint),
            Format = Format.FormatR32Uint
        };
        commandList.IASetIndexBuffer(ref indexBufferView);

        commandList.DrawIndexedInstanced((uint)indices.Length, 1, 0, 0, 0);
    }

    public void EndShadowPass()
    {
    }

    public void BindShadowResources(ReadOnlySpan<Matrix4x4> lightVPs, ReadOnlySpan<float> cascadeSplits)
    {
        if (!_gpuInitialized || _commandQueue is null) return;

        var commandList = _commandQueue.CommandList;

        if (!_arrayIsShaderVisible)
        {
            TransitionShadowArray(commandList, ResourceStates.DepthWrite, ResourceStates.PixelShaderResource);
            _arrayIsShaderVisible = true;
        }

        ((DirectX12Device)_device).RestoreBackBufferTarget();

        _shadowMapManager.SetShadowConstants(ShadowCbuffer.Create(lightVPs, cascadeSplits));

        if (_logger?.IsEnabled(LogLevel.Debug) == true)
        {
            _logger.LogDebug(
                "Shadow map cascades captured: {CascadeCount} splits, resolution {Resolution}",
                lightVPs.Length, _shadowMapManager.Resolution);
        }
    }

    private void EnsureGpuResources()
    {
        if (_gpuInitialized) return;

        if (!_device.IsInitialized)
        {
            throw new InvalidOperationException(
                "DirectX12ShadowRenderer.BeginShadowPass was called before the graphics device was initialized.");
        }

        var dx12Device = (DirectX12Device)_device;
        var d3dDevice = dx12Device.GetDevice();

        if (!_shadowMapManager.IsInitialized)
        {
            _shadowMapManager.Initialize(d3dDevice, _settings.Resolution, _settings.CascadeCount);
        }

        if (!_pipelineStateManager.IsInitialized)
        {
            _pipelineStateManager.Initialize(d3dDevice, _settings);
        }

        _commandQueue = dx12Device.GetDirectX12CommandQueue();

        CreateVertexBuffer(d3dDevice);
        CreateIndexBuffer(d3dDevice);

        _gpuInitialized = true;

        if (_logger?.IsEnabled(LogLevel.Debug) == true)
        {
            _logger.LogDebug("DirectX12ShadowRenderer GPU resources initialized");
        }
    }

    private void TransitionShadowArray(ComPtr<ID3D12GraphicsCommandList> commandList, ResourceStates before, ResourceStates after)
    {
        var barrier = new ResourceBarrier
        {
            Type = ResourceBarrierType.Transition,
            Flags = ResourceBarrierFlags.None,
            Transition = new ResourceTransitionBarrier
            {
                PResource = _shadowMapManager.ShadowTexture,
                StateBefore = before,
                StateAfter = after,
                Subresource = D3D12.ResourceBarrierAllSubresources
            }
        };

        commandList.ResourceBarrier(1, ref barrier);
    }

    private static void WriteMatrix(Span<float> dest, int offset, in Matrix4x4 m)
    {
        dest[offset + 0] = m.M11;
        dest[offset + 1] = m.M12;
        dest[offset + 2] = m.M13;
        dest[offset + 3] = m.M14;
        dest[offset + 4] = m.M21;
        dest[offset + 5] = m.M22;
        dest[offset + 6] = m.M23;
        dest[offset + 7] = m.M24;
        dest[offset + 8] = m.M31;
        dest[offset + 9] = m.M32;
        dest[offset + 10] = m.M33;
        dest[offset + 11] = m.M34;
        dest[offset + 12] = m.M41;
        dest[offset + 13] = m.M42;
        dest[offset + 14] = m.M43;
        dest[offset + 15] = m.M44;
    }

    private void CreateVertexBuffer(ComPtr<ID3D12Device> device)
    {
        var bufferSize = (ulong)(FloatsPerVertex * sizeof(float) * MaxVertices);
        CreateUploadBuffer(device, bufferSize, out _vertexBuffer);
    }

    private void CreateIndexBuffer(ComPtr<ID3D12Device> device)
    {
        var bufferSize = (ulong)(sizeof(uint) * MaxIndices);
        CreateUploadBuffer(device, bufferSize, out _indexBuffer);
    }

    private static void CreateUploadBuffer(ComPtr<ID3D12Device> device, ulong bufferSize, out ComPtr<ID3D12Resource> buffer)
    {
        var heapProps = new HeapProperties
        {
            Type = HeapType.Upload,
            CPUPageProperty = CpuPageProperty.Unknown,
            MemoryPoolPreference = MemoryPool.Unknown
        };

        var resourceDesc = new ResourceDesc
        {
            Dimension = ResourceDimension.Buffer,
            Alignment = 0,
            Width = bufferSize,
            Height = 1,
            DepthOrArraySize = 1,
            MipLevels = 1,
            Format = Format.FormatUnknown,
            SampleDesc = new SampleDesc { Count = 1, Quality = 0 },
            Layout = TextureLayout.LayoutRowMajor,
            Flags = ResourceFlags.None
        };

        unsafe
        {
            var result = device.CreateCommittedResource(
                in heapProps,
                HeapFlags.None,
                in resourceDesc,
                ResourceStates.GenericRead,
                null,
                out buffer);

            if (result < 0)
                throw new InvalidOperationException($"Failed to create shadow pass buffer. HRESULT: {result:X8}");
        }
    }

    private unsafe void UploadVertexBuffer(ReadOnlySpan<float> vertices)
    {
        void* mappedData;
        var result = _vertexBuffer.Map(0u, (Range*)null, &mappedData);
        if (result < 0)
            throw new InvalidOperationException($"Failed to map shadow vertex buffer. HRESULT: {result:X8}");

        var dst = new Span<byte>(mappedData, vertices.Length * sizeof(float));
        MemoryMarshal.AsBytes(vertices).CopyTo(dst);

        _vertexBuffer.Unmap(0u, (Range*)null);
    }

    private unsafe void UploadIndexBuffer(ReadOnlySpan<uint> indices)
    {
        void* mappedData;
        var result = _indexBuffer.Map(0u, (Range*)null, &mappedData);
        if (result < 0)
            throw new InvalidOperationException($"Failed to map shadow index buffer. HRESULT: {result:X8}");

        var dst = new Span<byte>(mappedData, indices.Length * sizeof(uint));
        MemoryMarshal.AsBytes(indices).CopyTo(dst);

        _indexBuffer.Unmap(0u, (Range*)null);
    }

    public void Dispose()
    {
        if (_disposed) return;

        _vertexBuffer.Dispose();
        _indexBuffer.Dispose();
        _d3d12.Dispose();

        _disposed = true;
    }
}
