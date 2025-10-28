using System.Numerics;
using System.Runtime.InteropServices;
using HEngine.Rendering.Data;
using HEngine.Rendering.Diagnostics;
using HEngine.Rendering.DirectX12;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using Range = Silk.NET.Direct3D12.Range;

namespace HEngine.Rendering.Managers;

public class DirectX12BufferManager : IDisposable
{
    private UploadRingBuffer? _uploadRingBuffer;

    private const int FrameCount = 3;
    private const uint MaxVertices = 1024 * 6;
    private ComPtr<ID3D12Resource> _constantBuffer;
    private IntPtr _persistentConstantBufferMapping;
    private bool _disposed;
    private readonly ComPtr<ID3D12Resource>[] _vertexBuffers = new ComPtr<ID3D12Resource>[FrameCount];
    private readonly IntPtr[] _persistentVertexMappings = new IntPtr[FrameCount];
    private int _currentVertexBufferIndex;

    private Matrix4x4 _lastViewMatrix;
    private Matrix4x4 _lastProjectionMatrix;
    private bool _constantBufferDirty;

    public RenderingMetrics? Metrics { get; set; }
    public ComPtr<ID3D12Resource> ConstantBuffer => _constantBuffer;

    public void Dispose()
    {
        if (_disposed) return;

        _uploadRingBuffer?.Dispose();

        unsafe
        {
            for (int i = 0; i < FrameCount; i++)
            {
                if (_persistentVertexMappings[i] != IntPtr.Zero)
                {
                    _vertexBuffers[i].Unmap(0u, (Range*)null);
                    _persistentVertexMappings[i] = IntPtr.Zero;
                }
                _vertexBuffers[i].Dispose();
            }

            if (_persistentConstantBufferMapping != IntPtr.Zero)
            {
                _constantBuffer.Unmap(0u, (Range*)null);
                _persistentConstantBufferMapping = IntPtr.Zero;
            }
        }

        _constantBuffer.Dispose();
        _disposed = true;
    }

    public void Initialize(ComPtr<ID3D12Device> device, Vector2 screenSize)
    {
        _uploadRingBuffer = new UploadRingBuffer(frameCount: 3);
        _uploadRingBuffer.Initialize(device, sizeInBytes: 16 * 1024 * 1024);

        for (int i = 0; i < FrameCount; i++)
        {
            CreateVertexBuffer(device, i);
        }

        CreateConstantBuffer(device);

        _lastViewMatrix = Matrix4x4.Identity;
        _lastProjectionMatrix = Matrix4x4.Identity;
        _constantBufferDirty = true;

        UpdateCameraConstants(Matrix4x4.Identity, Matrix4x4.Identity);
    }

    private void CreateVertexBuffer(ComPtr<ID3D12Device> device, int bufferIndex)
    {
        var vertexBufferSize = SpriteVertex.GetStride() * MaxVertices;

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
                out _vertexBuffers[bufferIndex]);

            if (result < 0)
                throw new Exception($"Failed to create vertex buffer {bufferIndex}. HRESULT: {result:X8}");

            void* mappedData;
            result = _vertexBuffers[bufferIndex].Map(0u, (Range*)null, &mappedData);
            if (result < 0)
                throw new Exception($"Failed to map vertex buffer {bufferIndex}. HRESULT: {result:X8}");

            _persistentVertexMappings[bufferIndex] = (IntPtr)mappedData;
        }
    }

    private void CreateConstantBuffer(ComPtr<ID3D12Device> device)
    {
        var size = Marshal.SizeOf<CameraConstants>();
        var constantBufferSize = (uint)((size + 255) & ~255);

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

            void* mappedData;
            result = _constantBuffer.Map(0u, (Range*)null, &mappedData);
            if (result < 0)
                throw new Exception($"Failed to map constant buffer. HRESULT: {result:X8}");

            _persistentConstantBufferMapping = (IntPtr)mappedData;
        }
    }

    public void UpdateCameraConstants(Matrix4x4 view, Matrix4x4 projection)
    {
        if (view == _lastViewMatrix && projection == _lastProjectionMatrix && !_constantBufferDirty)
        {
            Metrics?.IncrementConstantBufferSkips();
            return;
        }

        unsafe
        {
            var data = new CameraConstants { View = view, Projection = projection };
            var span = new Span<byte>((void*)_persistentConstantBufferMapping, Marshal.SizeOf<CameraConstants>());
            MemoryMarshal.Write(span, in data);
        }

        _lastViewMatrix = view;
        _lastProjectionMatrix = projection;
        _constantBufferDirty = false;

        Metrics?.IncrementConstantBufferUpdates();
    }

    public void MarkConstantBufferDirty()
    {
        _constantBufferDirty = true;
    }

    public void SetFrameIndex(int frameIndex)
    {
        if (frameIndex < 0 || frameIndex >= FrameCount)
            throw new ArgumentOutOfRangeException(nameof(frameIndex));

        _currentVertexBufferIndex = frameIndex;
    }

    public VertexBufferView GetCurrentVertexBufferView()
    {
        var vertexBufferSize = SpriteVertex.GetStride() * MaxVertices;
        return new VertexBufferView
        {
            BufferLocation = _vertexBuffers[_currentVertexBufferIndex].GetGPUVirtualAddress(),
            StrideInBytes = SpriteVertex.GetStride(),
            SizeInBytes = vertexBufferSize
        };
    }

    public void UpdateVertexBuffer(ReadOnlySpan<SpriteVertex> vertices)
    {
        if (vertices.Length > MaxVertices)
            throw new ArgumentException($"Too many vertices: {vertices.Length}, max: {MaxVertices}");

        unsafe
        {
            var vertexSize = (int)SpriteVertex.GetStride();
            var dst = new Span<byte>((void*)_persistentVertexMappings[_currentVertexBufferIndex], vertexSize * vertices.Length);

            MemoryMarshal.AsBytes(vertices).CopyTo(dst);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CameraConstants
    {
        public Matrix4x4 View;
        public Matrix4x4 Projection;
    }
}