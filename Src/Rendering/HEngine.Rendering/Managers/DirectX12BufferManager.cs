using System.Numerics;
using System.Runtime.InteropServices;
using HEngine.Rendering.Data;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using Range = Silk.NET.Direct3D12.Range;

namespace HEngine.Rendering.Managers;

public class DirectX12BufferManager : IDisposable
{
    private readonly uint _maxVertices = 1024 * 6;
    private ComPtr<ID3D12Resource> _constantBuffer;
    private bool _disposed;
    private ComPtr<ID3D12Resource> _vertexBuffer;

    public VertexBufferView VertexBufferView { get; private set; }
    public ComPtr<ID3D12Resource> ConstantBuffer => _constantBuffer;

    public void Dispose()
    {
        if (_disposed) return;

        _vertexBuffer.Dispose();
        _constantBuffer.Dispose();
        _disposed = true;
    }

    public void Initialize(ComPtr<ID3D12Device> device, Vector2 screenSize)
    {
        CreateVertexBuffer(device);
        CreateConstantBuffer(device, screenSize);
    }

    private void CreateVertexBuffer(ComPtr<ID3D12Device> device)
    {
        var vertexBufferSize = SpriteVertex.GetStride() * _maxVertices;

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
            Width = vertexBufferSize,
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
                out _vertexBuffer);

            if (result < 0)
                throw new Exception($"Failed to create vertex buffer. HRESULT: {result:X8}");
        }

        VertexBufferView = new VertexBufferView
        {
            BufferLocation = _vertexBuffer.GetGPUVirtualAddress(),
            StrideInBytes = SpriteVertex.GetStride(),
            SizeInBytes = vertexBufferSize
        };
    }

    private void CreateConstantBuffer(ComPtr<ID3D12Device> device, Vector2 screenSize)
    {
        var constantBufferSize = (uint)((Marshal.SizeOf<Vector2>() + 255) & ~255);

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
            var result = device.CreateCommittedResource(
                in heapProps,
                HeapFlags.None,
                in resourceDesc,
                ResourceStates.GenericRead,
                null,
                out _constantBuffer);

            if (result < 0)
                throw new Exception($"Failed to create constant buffer. HRESULT: {result:X8}");
        }

        UpdateConstantBuffer(screenSize);
    }

    private void UpdateConstantBuffer(Vector2 screenSize)
    {
        unsafe
        {
            void* mappedData;
            // Użyj uint zamiast int i Silk.NET.Direct3D12.Range
            var result = _constantBuffer.Map(0u, (Range*)null, &mappedData);
            if (result < 0)
                throw new Exception($"Failed to map constant buffer. HRESULT: {result:X8}");

            *(Vector2*)mappedData = screenSize;
            _constantBuffer.Unmap(0u, (Range*)null);
        }
    }

    public void UpdateVertexBuffer(SpriteVertex[] vertices)
    {
        if (vertices.Length > _maxVertices)
            throw new ArgumentException($"Too many vertices: {vertices.Length}, max: {_maxVertices}");

        unsafe
        {
            void* mappedData;
            // Użyj uint zamiast int i Silk.NET.Direct3D12.Range
            var result = _vertexBuffer.Map(0u, (Range*)null, &mappedData);
            if (result < 0)
                throw new Exception($"Failed to map vertex buffer. HRESULT: {result:X8}");

            var vertexSize = (int)SpriteVertex.GetStride();
            var src = new ReadOnlySpan<SpriteVertex>(vertices);
            var dst = new Span<byte>(mappedData, vertexSize * vertices.Length);

            MemoryMarshal.AsBytes(src).CopyTo(dst);

            _vertexBuffer.Unmap(0u, (Range*)null);
        }
    }
}