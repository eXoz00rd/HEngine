using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;

namespace HEngine.Rendering.Devices;

/// <summary>
/// Manages DX12 descriptor heaps for CBV/SRV/UAV and Samplers.
/// Provides O(1) alloc/dealloc via a free-list allocator.
/// </summary>
public sealed class DescriptorHeapManager : IDisposable
{
    private ComPtr<ID3D12DescriptorHeap> _srvHeap;
    private ComPtr<ID3D12DescriptorHeap> _samplerHeap;
    private ComPtr<ID3D12DescriptorHeap> _stagingHeap;

    private DescriptorAllocator? _srvAllocator;
    private DescriptorAllocator? _samplerAllocator;
    private DescriptorAllocator? _stagingAllocator;

    private bool _initialized;
    private bool _disposed;

    public const int DefaultSrvHeapSize = 4096;
    public const int DefaultSamplerHeapSize = 64;
    public const int DefaultStagingHeapSize = 256;

    public bool IsInitialized => _initialized;

    public void Initialize(ComPtr<ID3D12Device> device,
        int srvHeapSize = DefaultSrvHeapSize,
        int samplerHeapSize = DefaultSamplerHeapSize,
        int stagingHeapSize = DefaultStagingHeapSize)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(DescriptorHeapManager));
        if (_initialized)
            throw new InvalidOperationException("DescriptorHeapManager is already initialized.");

        _srvHeap = CreateHeap(device, DescriptorHeapType.CbvSrvUav, srvHeapSize, shaderVisible: true);
        _srvAllocator = new DescriptorAllocator(device, _srvHeap, DescriptorHeapType.CbvSrvUav, srvHeapSize);

        _samplerHeap = CreateHeap(device, DescriptorHeapType.Sampler, samplerHeapSize, shaderVisible: true);
        _samplerAllocator = new DescriptorAllocator(device, _samplerHeap, DescriptorHeapType.Sampler, samplerHeapSize);

        _stagingHeap = CreateHeap(device, DescriptorHeapType.CbvSrvUav, stagingHeapSize, shaderVisible: false);
        _stagingAllocator = new DescriptorAllocator(device, _stagingHeap, DescriptorHeapType.CbvSrvUav, stagingHeapSize);

        _initialized = true;
    }

    public DescriptorHandle AllocateSrv()
    {
        EnsureInitialized();
        return _srvAllocator!.Allocate();
    }

    public void FreeSrv(DescriptorHandle handle)
    {
        EnsureInitialized();
        _srvAllocator!.Free(handle);
    }

    public DescriptorHandle AllocateSampler()
    {
        EnsureInitialized();
        return _samplerAllocator!.Allocate();
    }

    public void FreeSampler(DescriptorHandle handle)
    {
        EnsureInitialized();
        _samplerAllocator!.Free(handle);
    }

    public DescriptorHandle AllocateStaging()
    {
        EnsureInitialized();
        return _stagingAllocator!.Allocate();
    }

    public void FreeStaging(DescriptorHandle handle)
    {
        EnsureInitialized();
        _stagingAllocator!.Free(handle);
    }

    public int SrvAllocatedCount => _srvAllocator?.AllocatedCount ?? 0;
    public int SamplerAllocatedCount => _samplerAllocator?.AllocatedCount ?? 0;
    public int StagingAllocatedCount => _stagingAllocator?.AllocatedCount ?? 0;
    public int SrvCapacity => _srvAllocator?.Capacity ?? 0;
    public int SamplerCapacity => _samplerAllocator?.Capacity ?? 0;

    public ComPtr<ID3D12DescriptorHeap> SrvHeap
    {
        get
        {
            EnsureInitialized();
            return _srvHeap;
        }
    }

    public ComPtr<ID3D12DescriptorHeap> SamplerHeap
    {
        get
        {
            EnsureInitialized();
            return _samplerHeap;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _stagingHeap.Dispose();
        _samplerHeap.Dispose();
        _srvHeap.Dispose();
        _disposed = true;
    }

    private void EnsureInitialized()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(DescriptorHeapManager));
        if (!_initialized)
            throw new InvalidOperationException("DescriptorHeapManager is not initialized. Call Initialize() first.");
    }

    private static ComPtr<ID3D12DescriptorHeap> CreateHeap(
        ComPtr<ID3D12Device> device,
        DescriptorHeapType type,
        int count,
        bool shaderVisible)
    {
        var desc = new DescriptorHeapDesc
        {
            Type = type,
            NumDescriptors = (uint)count,
            Flags = shaderVisible ? DescriptorHeapFlags.ShaderVisible : DescriptorHeapFlags.None,
            NodeMask = 0
        };

        var result = device.CreateDescriptorHeap(in desc, out ComPtr<ID3D12DescriptorHeap> heap);
        if (result < 0)
            throw new Exception($"Failed to create descriptor heap ({type}). HRESULT: {result:X8}");

        return heap;
    }
}

/// <summary>
/// A single descriptor location with CPU and GPU handles + index.
/// </summary>
public readonly struct DescriptorHandle : IEquatable<DescriptorHandle>
{
    public CpuDescriptorHandle CpuHandle { get; init; }
    public GpuDescriptorHandle GpuHandle { get; init; }
    public int Index { get; init; }

    public bool IsValid => Index >= 0;

    public static DescriptorHandle Invalid => new() { Index = -1 };

    public bool Equals(DescriptorHandle other) => Index == other.Index;
    public override bool Equals(object? obj) => obj is DescriptorHandle other && Equals(other);
    public override int GetHashCode() => Index;
    public static bool operator ==(DescriptorHandle left, DescriptorHandle right) => left.Equals(right);
    public static bool operator !=(DescriptorHandle left, DescriptorHandle right) => !left.Equals(right);
}

/// <summary>
/// Free-list based descriptor allocator. O(1) alloc/dealloc.
/// </summary>
internal sealed class DescriptorAllocator
{
    private readonly CpuDescriptorHandle _cpuStart;
    private readonly GpuDescriptorHandle _gpuStart;
    private readonly uint _incrementSize;
    private readonly Stack<int> _freeList;
    private readonly int _capacity;
    private int _allocatedCount;

    public int AllocatedCount => _allocatedCount;
    public int Capacity => _capacity;

    public DescriptorAllocator(
        ComPtr<ID3D12Device> device,
        ComPtr<ID3D12DescriptorHeap> heap,
        DescriptorHeapType type,
        int capacity)
    {
        _capacity = capacity;
        _incrementSize = device.GetDescriptorHandleIncrementSize(type);

        _cpuStart = heap.GetCPUDescriptorHandleForHeapStart();
        _gpuStart = heap.GetGPUDescriptorHandleForHeapStart();

        _freeList = new Stack<int>(capacity);

        // Push indices in reverse so that index 0 is popped first
        for (int i = capacity - 1; i >= 0; i--)
            _freeList.Push(i);
    }

    public DescriptorHandle Allocate()
    {
        if (_freeList.Count == 0)
            throw new InvalidOperationException(
                $"Descriptor heap exhausted. Capacity: {_capacity}, Allocated: {_allocatedCount}");

        var index = _freeList.Pop();
        _allocatedCount++;

        return new DescriptorHandle
        {
            CpuHandle = new CpuDescriptorHandle(_cpuStart.Ptr + (nuint)(index * _incrementSize)),
            GpuHandle = new GpuDescriptorHandle(_gpuStart.Ptr + (ulong)(index * _incrementSize)),
            Index = index
        };
    }

    public void Free(DescriptorHandle handle)
    {
        if (handle.Index < 0 || handle.Index >= _capacity)
            throw new ArgumentOutOfRangeException(nameof(handle), $"Invalid descriptor index: {handle.Index}");

        _freeList.Push(handle.Index);
        _allocatedCount--;
    }
}

