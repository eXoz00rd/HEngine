using System.Numerics;
using System.Runtime.InteropServices;
using HEngine.Core.Rendering.Contracts;
using HEngine.Core.Rendering.Data;
using HEngine.Rendering.Data;
using HEngine.Rendering.DirectX12;
using HEngine.Rendering.Enums;
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
    private ComPtr<ID3D12Resource> _sceneConstantBuffer;
    private ComPtr<ID3D12Resource> _materialConstantBuffer;
    private ComPtr<ID3D12Resource> _lightConstantBuffer;
    private ComPtr<ID3D12Resource> _shadowConstantBuffer;
    private unsafe void* _sceneConstantBufferMapped;
    private unsafe void* _materialConstantBufferMapped;
    private unsafe void* _lightConstantBufferMapped;
    private unsafe void* _shadowConstantBufferMapped;
    private ShadowMapManager? _shadowMapManager;
    private bool _useShadows;
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

    public void Initialize(object? device = null, ShadowMapManager? shadowMapManager = null, bool useShadows = false)
    {
        _shadowMapManager = shadowMapManager;
        _useShadows = useShadows;

        if (device is ComPtr<ID3D12Device> d3dDevice)
        {
            _device = d3dDevice;

            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            var shaderPath = Path.Combine(basePath, "Shaders");
            _shaderFileLoader = new ShaderFileLoader(shaderPath);

            var cachePath = Path.Combine(basePath, "ShaderCache");
            var diskCache = new ShaderDiskCache(cachePath);

            _shaderManager = new DirectX12MeshShaderManager(_shaderFileLoader, diskCache, null);
            var variant = useShadows
                ? new ShaderVariant(ShaderFeatureFlags.UseShadows)
                : new ShaderVariant(ShaderFeatureFlags.None);
            _shaderManager.Initialize(variant);

            _pipelineManager = new DirectX12MeshPipelineManager();
            _pipelineManager.Initialize(_device, _shaderManager);

            CreateVertexBuffer();
            CreateIndexBuffer();
            CreateSceneConstantBuffer();
            CreateMaterialConstantBuffer();
            CreateLightConstantBuffer();

            if (useShadows)
                CreateShadowConstantBuffer();

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
                         IRenderContext context,
                         Material? material = null,
                         LightData[]? lights = null)
    {
        ArgumentNullException.ThrowIfNull(context);

        var view = context.ViewMatrix;
        var proj = context.ProjectionMatrix;
        LastMvp = modelMatrix * view * proj;
        LastDrawVertexCount = vertices.Length;
        LastDrawIndexCount = indices.Length;

        if (!IsInitialized)
            return;

        if (vertices.Length > MaxVertices || indices.Length > MaxIndices)
            throw new ArgumentException("Too many vertices or indices");

        // If GPU resources or command queue are not available (headless mode), stop after updating metadata
        if (!_gpuResourcesCreated || _commandQueue == null)
            return;

        Matrix4x4.Invert(modelMatrix, out var invWorld);
        var normalMatrix = Matrix4x4.Transpose(invWorld);

        var sceneAddress = UpdateSceneConstantBuffer(modelMatrix, view, proj, normalMatrix, context);
        var materialAddress = UpdateMaterialConstantBuffer(material);
        var lightAddress = UpdateLightConstantBuffer(lights);

        UpdateVertexBuffer(vertices);
        UpdateIndexBuffer(indices);

        var commandList = _commandQueue.CommandList;

        commandList.SetPipelineState(_pipelineManager!.PipelineState);
        commandList.SetGraphicsRootSignature(_pipelineManager.RootSignature);
        commandList.SetGraphicsRootConstantBufferView(0, sceneAddress);
        commandList.SetGraphicsRootConstantBufferView(1, materialAddress);
        commandList.SetGraphicsRootConstantBufferView(2, lightAddress);

        if (_useShadows && _shadowMapManager is { IsInitialized: true })
        {
            var shadowAddress = UpdateShadowConstantBuffer();
            var shadowSrvHeap = _shadowMapManager.SrvHeap;
            commandList.SetDescriptorHeaps(1, ref shadowSrvHeap);
            commandList.SetGraphicsRootDescriptorTable(3, _shadowMapManager.GetSrvGpuHandle());
            commandList.SetGraphicsRootConstantBufferView(4, shadowAddress);
        }

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

    private unsafe void CreateSceneConstantBuffer()
    {
        CreateMappedConstantBuffer<PBRSceneConstants>(MaxDrawCalls, out _sceneConstantBuffer, out _sceneConstantBufferMapped);
    }

    private unsafe void CreateMaterialConstantBuffer()
    {
        CreateMappedConstantBuffer<PBRMaterialConstants>(MaxDrawCalls, out _materialConstantBuffer, out _materialConstantBufferMapped);
    }

    private unsafe void CreateLightConstantBuffer()
    {
        var alignedSize = (PBRLightLayout.TotalSize + 255) & ~255;
        var totalSize = (ulong)(alignedSize * MaxDrawCalls);
        CreateMappedRawBuffer(totalSize, out _lightConstantBuffer, out _lightConstantBufferMapped);
    }

    private unsafe void CreateShadowConstantBuffer()
    {
        CreateMappedConstantBuffer<ShadowCbuffer>(MaxDrawCalls, out _shadowConstantBuffer, out _shadowConstantBufferMapped);
    }

    private unsafe void CreateMappedConstantBuffer<T>(int slotCount, out ComPtr<ID3D12Resource> buffer, out void* mapped) where T : unmanaged
    {
        var size = sizeof(T);
        var alignedSize = (size + 255) & ~255;
        var totalSize = (ulong)(alignedSize * slotCount);
        CreateMappedRawBuffer(totalSize, out buffer, out mapped);
    }

    private unsafe void CreateMappedRawBuffer(ulong totalSize, out ComPtr<ID3D12Resource> buffer, out void* mapped)
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
            Width = totalSize,
            Height = 1,
            DepthOrArraySize = 1,
            MipLevels = 1,
            Format = Format.FormatUnknown,
            SampleDesc = new SampleDesc { Count = 1, Quality = 0 },
            Layout = TextureLayout.LayoutRowMajor,
            Flags = ResourceFlags.None
        };

        buffer = default;
        var result = _device.CreateCommittedResource(
            in heapProps,
            HeapFlags.None,
            in resourceDesc,
            ResourceStates.GenericRead,
            null,
            out buffer);

        if (result < 0)
            throw new Exception($"Failed to create constant buffer. HRESULT: {result:X8}");

        void* mappedPtr;
        result = buffer.Map(0u, (Range*)null, &mappedPtr);
        if (result < 0)
            throw new Exception($"Failed to map constant buffer. HRESULT: {result:X8}");

        mapped = mappedPtr;
    }

    private unsafe ulong UpdateSceneConstantBuffer(
        Matrix4x4 world, Matrix4x4 view, Matrix4x4 proj, Matrix4x4 normalMatrix, IRenderContext context)
    {
        var alignedSize = (sizeof(PBRSceneConstants) + 255) & ~255;
        var offset = _currentDrawCallIndex * alignedSize;

        var cameraPos = Vector3.Zero;
        if (Matrix4x4.Invert(view, out var invView))
            cameraPos = new Vector3(invView.M41, invView.M42, invView.M43);

        var constants = new PBRSceneConstants
        {
            World = world,
            View = view,
            Projection = proj,
            WorldViewProjection = world * view * proj,
            NormalMatrix = normalMatrix,
            CameraPosition = cameraPos,
            Pad0 = 0f
        };

        var destPtr = (byte*)_sceneConstantBufferMapped + offset;
        MemoryMarshal.Write(new Span<byte>(destPtr, sizeof(PBRSceneConstants)), in constants);

        return _sceneConstantBuffer.GetGPUVirtualAddress() + (ulong)offset;
    }

    private unsafe ulong UpdateMaterialConstantBuffer(Material? material)
    {
        var alignedSize = (sizeof(PBRMaterialConstants) + 255) & ~255;
        var offset = _currentDrawCallIndex * alignedSize;

        var constants = material is not null
            ? MaterialConstantsSerializer.ToGpu(material)
            : MaterialConstantsSerializer.Default();

        var destPtr = (byte*)_materialConstantBufferMapped + offset;
        MemoryMarshal.Write(new Span<byte>(destPtr, sizeof(PBRMaterialConstants)), in constants);

        return _materialConstantBuffer.GetGPUVirtualAddress() + (ulong)offset;
    }

    private unsafe ulong UpdateShadowConstantBuffer()
    {
        var alignedSize = (sizeof(ShadowCbuffer) + 255) & ~255;
        var offset = _currentDrawCallIndex * alignedSize;

        var constants = _shadowMapManager!.ShadowConstants;

        var destPtr = (byte*)_shadowConstantBufferMapped + offset;
        MemoryMarshal.Write(new Span<byte>(destPtr, sizeof(ShadowCbuffer)), in constants);

        return _shadowConstantBuffer.GetGPUVirtualAddress() + (ulong)offset;
    }

    private unsafe ulong UpdateLightConstantBuffer(LightData[]? lights)
    {
        var alignedSize = (PBRLightLayout.TotalSize + 255) & ~255;
        var offset = _currentDrawCallIndex * alignedSize;

        var destPtr = (byte*)_lightConstantBufferMapped + offset;
        new Span<byte>(destPtr, alignedSize).Clear();

        var count = Math.Min(lights?.Length ?? 0, PBRLightLayout.MaxLights);

        if (lights != null)
        {
            for (var i = 0; i < count; i++)
            {
                var l = lights[i];
                var gpuLight = new PBRLightGpu
                {
                    Color = l.Color,
                    Intensity = l.Intensity,
                    Direction = l.Direction,
                    Range = l.Range,
                    Position = l.Position,
                    Type = (int)l.Type,
                    InnerConeAngle = l.InnerConeAngle,
                    OuterConeAngle = l.OuterConeAngle,
                    Pad = Vector2.Zero
                };
                MemoryMarshal.Write(new Span<byte>(destPtr + i * sizeof(PBRLightGpu), sizeof(PBRLightGpu)), in gpuLight);
            }
        }

        *(int*)(destPtr + PBRLightLayout.ActiveCountOffset) = count;
        var ambient = new Vector3(0.03f, 0.03f, 0.03f);
        MemoryMarshal.Write(new Span<byte>(destPtr + PBRLightLayout.AmbientColorOffset, sizeof(Vector3)), in ambient);

        return _lightConstantBuffer.GetGPUVirtualAddress() + (ulong)offset;
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
            if (_sceneConstantBufferMapped != null)
            {
                _sceneConstantBuffer.Unmap(0u, (Range*)null);
                _sceneConstantBufferMapped = null;
            }

            if (_materialConstantBufferMapped != null)
            {
                _materialConstantBuffer.Unmap(0u, (Range*)null);
                _materialConstantBufferMapped = null;
            }

            if (_lightConstantBufferMapped != null)
            {
                _lightConstantBuffer.Unmap(0u, (Range*)null);
                _lightConstantBufferMapped = null;
            }

            if (_shadowConstantBufferMapped != null)
            {
                _shadowConstantBuffer.Unmap(0u, (Range*)null);
                _shadowConstantBufferMapped = null;
            }
        }

        _shadowConstantBuffer.Dispose();
        _lightConstantBuffer.Dispose();
        _materialConstantBuffer.Dispose();
        _sceneConstantBuffer.Dispose();
        _indexBuffer.Dispose();
        _vertexBuffer.Dispose();
        _pipelineManager?.Dispose();
        _shaderManager?.Dispose();
        _shaderFileLoader?.Dispose();
        _d3d12.Dispose();

        IsInitialized = false;
        _disposed = true;
    }
}