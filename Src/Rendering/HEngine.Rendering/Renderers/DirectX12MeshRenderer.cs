using System.Numerics;
using System.Runtime.InteropServices;
using HEngine.Core.Rendering.Contracts;
using HEngine.Core.Rendering.Data;
using HEngine.Rendering.DirectX12;
using HEngine.Rendering.Managers;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using Range = Silk.NET.Direct3D12.Range;

namespace HEngine.Rendering.Renderers;

public sealed class DirectX12MeshRenderer : IDisposable
{
    private readonly D3D12 _d3d12 = D3D12.GetApi();
    private ComPtr<ID3D12Device> _device;
    private DirectX12CommandQueue? _commandQueue;
    private DirectX12MeshShaderManager? _shaderManager;
    private DirectX12MeshPipelineManager? _pipelineManager;
    private ShaderFileLoader? _shaderFileLoader;
    private ComPtr<ID3D12Resource> _vertexBuffer;
    private ComPtr<ID3D12Resource> _indexBuffer;
    private ComPtr<ID3D12Resource> _constantBuffer;
    private unsafe void* _constantBufferMapped;
    private const int MaxVertices = 65536;
    private const int MaxIndices = 65536 * 3;
    private const int MaxDrawCalls = 1024;
    private int _currentDrawCallIndex;
    private bool _disposed;
    private bool _gpuResourcesCreated;

    public bool IsInitialized { get; private set; }
    public bool DepthTestEnabled { get; private set; } = true;
    public bool BackFaceCullingEnabled { get; private set; } = true;

    public Matrix4x4 LastMvp { get; private set; } = Matrix4x4.Identity;
    public int LastDrawVertexCount { get; private set; }
    public int LastDrawIndexCount { get; private set; }

    public void Initialize(object? device = null)
    {
        // Allow initialization without a GPU device (headless/test mode).
        // When a valid D3D12 device is provided, create GPU resources; otherwise, skip GPU setup
        // but still mark the renderer as initialized so that CPU-side math and metadata updates work in tests.
        if (device is ComPtr<ID3D12Device> d3dDevice)
        {
            _device = d3dDevice;

            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            var shaderPath = Path.Combine(basePath, "Shaders");
            _shaderFileLoader = new ShaderFileLoader(shaderPath);

            var cachePath = Path.Combine(basePath, "ShaderCache");
            var diskCache = new ShaderDiskCache(cachePath);

            _shaderManager = new DirectX12MeshShaderManager(_shaderFileLoader, diskCache, null);
            _shaderManager.Initialize();

            _pipelineManager = new DirectX12MeshPipelineManager();
            _pipelineManager.Initialize(_device, _shaderManager);

            CreateVertexBuffer();
            CreateIndexBuffer();
            CreateConstantBuffer();

            _gpuResourcesCreated = true;
        }
        else if (device is not null)
        {
            throw new ArgumentException("Device must be ComPtr<ID3D12Device>", nameof(device));
        }

        IsInitialized = true;
    }

    public void SetCommandQueue(DirectX12CommandQueue commandQueue)
    {
        _commandQueue = commandQueue;
    }

    public void SetDepthTest(bool enabled) => DepthTestEnabled = enabled;
    public void SetBackFaceCulling(bool enabled) => BackFaceCullingEnabled = enabled;

    public void DrawMesh(Matrix4x4 modelMatrix,
                         ReadOnlySpan<Vertex3D> vertices,
                         ReadOnlySpan<uint> indices,
                         IRenderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Always compute CPU-side values so tests can assert on them even without a GPU device/queue
        LastMvp = modelMatrix * context.ViewMatrix * context.ProjectionMatrix;
        LastDrawVertexCount = vertices.Length;
        LastDrawIndexCount = indices.Length;

        if (!IsInitialized)
            return;

        if (vertices.Length > MaxVertices || indices.Length > MaxIndices)
            throw new ArgumentException("Too many vertices or indices");

        // If GPU resources or command queue are not available (headless mode), stop after updating metadata
        if (!_gpuResourcesCreated || _commandQueue == null)
            return;

        var constantBufferAddress = UpdateConstantBuffer(LastMvp);
        UpdateVertexBuffer(vertices);
        UpdateIndexBuffer(indices);

        var commandList = _commandQueue.CommandList;

        commandList.SetPipelineState(_pipelineManager!.PipelineState);
        commandList.SetGraphicsRootSignature(_pipelineManager.RootSignature);
        commandList.SetGraphicsRootConstantBufferView(0, constantBufferAddress);

        commandList.IASetPrimitiveTopology(D3DPrimitiveTopology.D3DPrimitiveTopologyTrianglelist);

        var vertexBufferView = new VertexBufferView
        {
            BufferLocation = _vertexBuffer.GetGPUVirtualAddress(),
            StrideInBytes = Vertex3D.GetStride(),
            SizeInBytes = Vertex3D.GetStride() * (uint)vertices.Length
        };

        commandList.IASetVertexBuffers(0, 1, ref vertexBufferView);

        var indexBufferView = new IndexBufferView
        {
            BufferLocation = _indexBuffer.GetGPUVirtualAddress(),
            SizeInBytes = sizeof(uint) * (uint)indices.Length,
            Format = Format.FormatR32Uint
        };

        commandList.IASetIndexBuffer(ref indexBufferView);

        commandList.DrawIndexedInstanced((uint)indices.Length, 1, 0, 0, 0);

        _currentDrawCallIndex++;
        if (_currentDrawCallIndex >= MaxDrawCalls)
            throw new InvalidOperationException($"Exceeded maximum draw calls per frame ({MaxDrawCalls})");
    }

    public void BeginFrame()
    {
        _currentDrawCallIndex = 0;
    }

    private void CreateVertexBuffer()
    {
        var bufferSize = Vertex3D.GetStride() * MaxVertices;

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
            var result = _device.CreateCommittedResource(
                in heapProps,
                HeapFlags.None,
                in resourceDesc,
                ResourceStates.GenericRead,
                null,
                out _vertexBuffer);

            if (result < 0)
                throw new Exception($"Failed to create mesh vertex buffer. HRESULT: {result:X8}");
        }
    }

    private void CreateIndexBuffer()
    {
        var bufferSize = (ulong)(sizeof(uint) * MaxIndices);

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
            var result = _device.CreateCommittedResource(
                in heapProps,
                HeapFlags.None,
                in resourceDesc,
                ResourceStates.GenericRead,
                null,
                out _indexBuffer);

            if (result < 0)
                throw new Exception($"Failed to create mesh index buffer. HRESULT: {result:X8}");
        }
    }

    private void CreateConstantBuffer()
    {
        var size = Marshal.SizeOf<MeshConstants>();
        var alignedSize = (size + 255) & ~255;
        var constantBufferSize = (ulong)(alignedSize * MaxDrawCalls);

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
            Width = constantBufferSize,
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
            var result = _device.CreateCommittedResource(
                in heapProps,
                HeapFlags.None,
                in resourceDesc,
                ResourceStates.GenericRead,
                null,
                out _constantBuffer);

            if (result < 0)
                throw new Exception($"Failed to create mesh constant buffer. HRESULT: {result:X8}");

            void* mappedPtr;
            result = _constantBuffer.Map(0u, (Range*)null, &mappedPtr);
            if (result < 0)
                throw new Exception($"Failed to map mesh constant buffer. HRESULT: {result:X8}");

            _constantBufferMapped = mappedPtr;
        }
    }

    private ulong UpdateConstantBuffer(Matrix4x4 mvp)
    {
        var size = Marshal.SizeOf<MeshConstants>();
        var alignedSize = (size + 255) & ~255;
        var offset = _currentDrawCallIndex * alignedSize;

        unsafe
        {
            var constants = new MeshConstants
            {
                MVP = mvp,
                LightDirection = new Vector4(0.5f, -1.0f, 0.5f, 0.0f),
                LightColor = new Vector4(1.0f, 1.0f, 1.0f, 1.0f),
                AmbientColor = new Vector4(0.2f, 0.2f, 0.2f, 1.0f)
            };

            var destPtr = (byte*)_constantBufferMapped + offset;
            var span = new Span<byte>(destPtr, size);
            MemoryMarshal.Write(span, in constants);
        }

        return _constantBuffer.GetGPUVirtualAddress() + (ulong)offset;
    }

    private void UpdateVertexBuffer(ReadOnlySpan<Vertex3D> vertices)
    {
        unsafe
        {
            void* mappedData;
            var result = _vertexBuffer.Map(0u, (Range*)null, &mappedData);
            if (result < 0)
                throw new Exception($"Failed to map mesh vertex buffer. HRESULT: {result:X8}");

            var vertexSize = (int)Vertex3D.GetStride();
            var dst = new Span<byte>(mappedData, vertexSize * vertices.Length);

            MemoryMarshal.AsBytes(vertices).CopyTo(dst);

            _vertexBuffer.Unmap(0u, (Range*)null);
        }
    }

    private void UpdateIndexBuffer(ReadOnlySpan<uint> indices)
    {
        unsafe
        {
            void* mappedData;
            var result = _indexBuffer.Map(0u, (Range*)null, &mappedData);
            if (result < 0)
                throw new Exception($"Failed to map mesh index buffer. HRESULT: {result:X8}");

            var indexSize = sizeof(uint);
            var dst = new Span<byte>(mappedData, indexSize * indices.Length);

            MemoryMarshal.AsBytes(indices).CopyTo(dst);

            _indexBuffer.Unmap(0u, (Range*)null);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        unsafe
        {
            if (_constantBufferMapped != null)
            {
                _constantBuffer.Unmap(0u, (Range*)null);
                _constantBufferMapped = null;
            }
        }

        _constantBuffer.Dispose();
        _indexBuffer.Dispose();
        _vertexBuffer.Dispose();
        _pipelineManager?.Dispose();
        _shaderManager?.Dispose();
        _shaderFileLoader?.Dispose();
        _d3d12.Dispose();

        IsInitialized = false;
        _disposed = true;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MeshConstants
    {
        public Matrix4x4 MVP;
        public Vector4 LightDirection;
        public Vector4 LightColor;
        public Vector4 AmbientColor;
    }
}