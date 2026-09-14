using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using Range = Silk.NET.Direct3D12.Range;

namespace HEngine.Rendering.DirectX12;

public unsafe class UploadRingBuffer : IDisposable
{
    private readonly int _frameCount;

    private readonly ulong[] _frameOffsets;

    private ComPtr<ID3D12Resource> _buffer;
    private byte* _cpuMappedAddress;
    private bool _disposed;

    public UploadRingBuffer(int frameCount)
    {
        _frameCount = frameCount;
        _frameOffsets = new ulong[frameCount];
    }

    public ulong GpuBaseAddress { get; private set; }

    public ulong BufferSize { get; private set; }

    public ulong CurrentOffset { get; private set; }

    public void Dispose()
    {
        if (_disposed)
            return;

        if (_cpuMappedAddress != null)
        {
            Range* nullRange = null;
            _buffer.Unmap(0, nullRange);
        }

        _buffer.Dispose();
        _disposed = true;
    }

    public void Initialize(ComPtr<ID3D12Device> device, ulong sizeInBytes)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(UploadRingBuffer));

        BufferSize = sizeInBytes;

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
            Width = sizeInBytes,
            Height = 1,
            DepthOrArraySize = 1,
            MipLevels = 1,
            Format = Format.FormatUnknown,
            SampleDesc = new SampleDesc { Count = 1, Quality = 0 },
            Layout = TextureLayout.LayoutRowMajor,
            Flags = ResourceFlags.None
        };

        var result = device.CreateCommittedResource(
            in heapProps,
            HeapFlags.None,
            in resourceDesc,
            ResourceStates.GenericRead,
            null,
            out _buffer);

        if (result < 0)
            throw new Exception($"Failed to create upload ring buffer. HRESULT: {result:X8}");

        void* mappedPtr;
        Range* nullRange = null;
        result = _buffer.Map(0, nullRange, &mappedPtr);
        if (result < 0)
            throw new Exception($"Failed to map upload ring buffer. HRESULT: {result:X8}");

        _cpuMappedAddress = (byte*)mappedPtr;
        GpuBaseAddress = _buffer.GetGPUVirtualAddress();

        for (var i = 0; i < _frameCount; i++)
            _frameOffsets[i] = 0;
    }
    
    public GpuUploadAllocation Allocate(ulong sizeInBytes, ulong alignment = 256)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(UploadRingBuffer));
        
        CurrentOffset = AlignUp(CurrentOffset, alignment);

        if (CurrentOffset + sizeInBytes > BufferSize)
            throw new OutOfMemoryException(
                $"Upload ring buffer exhausted! Requested: {sizeInBytes} bytes, " +
                $"Available: {BufferSize - CurrentOffset} bytes. " +
                $"Consider increasing buffer size or flushing more frequently.");

        var allocation = new GpuUploadAllocation
        {
            CpuAddress = _cpuMappedAddress + CurrentOffset,
            GpuAddress = GpuBaseAddress + CurrentOffset,
            Offset = CurrentOffset,
            Size = sizeInBytes
        };

        CurrentOffset += sizeInBytes;

        return allocation;
    }

    public void FinishFrame(int frameIndex)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(UploadRingBuffer));

        if (frameIndex < 0 || frameIndex >= _frameCount)
            throw new ArgumentOutOfRangeException(nameof(frameIndex));

        _frameOffsets[frameIndex] = CurrentOffset;
    }

    public void BeginFrame(int frameIndex)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(UploadRingBuffer));

        if (frameIndex < 0 || frameIndex >= _frameCount)
            throw new ArgumentOutOfRangeException(nameof(frameIndex));

        CurrentOffset = _frameOffsets[frameIndex];
    }
    
    public float GetUtilizationPercent()
    {
        return CurrentOffset / (float)BufferSize * 100f;
    }

    private static ulong AlignUp(ulong value, ulong alignment)
    {
        return (value + alignment - 1) & ~(alignment - 1);
    }
}

public unsafe struct GpuUploadAllocation
{
    public byte* CpuAddress;

    public ulong GpuAddress;

    public ulong Offset;

    public ulong Size;

    public void WriteData<T>(in T data) where T : unmanaged
    {
        *(T*)CpuAddress = data;
    }

    public void WriteData<T>(ReadOnlySpan<T> data) where T : unmanaged
    {
        var byteSize = data.Length * sizeof(T);
        if ((ulong)byteSize > Size)
            throw new ArgumentException("Data too large for allocation");

        fixed (T* srcPtr = data)
        {
            Buffer.MemoryCopy(srcPtr, CpuAddress, (long)Size, byteSize);
        }
    }

    public void WriteBytes(ReadOnlySpan<byte> data)
    {
        if ((ulong)data.Length > Size)
            throw new ArgumentException("Data too large for allocation");

        fixed (byte* srcPtr = data)
        {
            Buffer.MemoryCopy(srcPtr, CpuAddress, (long)Size, data.Length);
        }
    }
}