using HEngine.Core.Rendering.Contracts;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using System.Runtime.InteropServices;

namespace HEngine.Rendering.DirectX12;

public class DirectX12CommandQueue : ICommandQueue
{
    private const int FRAME_COUNT = 3;

    private readonly D3D12 _d3d12 = D3D12.GetApi();
    private readonly ComPtr<ID3D12CommandAllocator>[] _commandAllocators = new ComPtr<ID3D12CommandAllocator>[FRAME_COUNT];
    private int _currentAllocatorIndex;
    private ComPtr<ID3D12GraphicsCommandList> _commandList;
    private ComPtr<ID3D12CommandQueue> _commandQueue;
    private ComPtr<ID3D12Fence> _fence;
    private IntPtr _fenceEvent;
    private ulong _fenceValue = 1;
    private readonly ulong[] _frameFenceValues = new ulong[FRAME_COUNT];
    private bool _disposed;
    
    public ComPtr<ID3D12CommandQueue> Queue => _commandQueue;
    public ComPtr<ID3D12GraphicsCommandList> CommandList => _commandList;

    public bool IsFrameInProgress { get; private set; }
    public bool IsCommandListOpen { get; private set; }

    public void BeginFrame()
    {
        BeginFrame(0);
    }

    public void BeginFrame(int frameIndex)
    {
        if (IsFrameInProgress)
            throw new InvalidOperationException("Frame already in progress");

        if (frameIndex < 0 || frameIndex >= FRAME_COUNT)
            throw new ArgumentOutOfRangeException(nameof(frameIndex));

        WaitForFrame(frameIndex);

        _currentAllocatorIndex = frameIndex;
        _commandAllocators[frameIndex].Reset();

        unsafe
        {
            _commandList.Reset(_commandAllocators[frameIndex], (ID3D12PipelineState*)null);
        }

        IsCommandListOpen = true;
        IsFrameInProgress = true;
    }

    public void EndFrame()
    {
        EndFrame(0);
    }

    public void EndFrame(int frameIndex)
    {
        if (!IsFrameInProgress)
            throw new InvalidOperationException("No frame in progress");

        if (frameIndex < 0 || frameIndex >= FRAME_COUNT)
            throw new ArgumentOutOfRangeException(nameof(frameIndex));

        if (IsCommandListOpen)
        {
            _commandList.Close();
            IsCommandListOpen = false;
        }

        unsafe
        {
            var commandLists = stackalloc ID3D12CommandList*[1];
            commandLists[0] = (ID3D12CommandList*)_commandList.Handle;
            _commandQueue.ExecuteCommandLists(1, commandLists);
        }

        _commandQueue.Signal(_fence, _fenceValue);
        _frameFenceValues[frameIndex] = _fenceValue;
        _fenceValue++;

        IsFrameInProgress = false;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        WaitForGpuIdle();

        if (_fenceEvent != IntPtr.Zero)
            CloseHandle(_fenceEvent);

        _fence.Dispose();
        _commandList.Dispose();

        for (int i = 0; i < FRAME_COUNT; i++)
            _commandAllocators[i].Dispose();

        _commandQueue.Dispose();
        _d3d12.Dispose();
        _disposed = true;
    }

    private void WaitForFrame(int frameIndex)
    {
        var targetFenceValue = _frameFenceValues[frameIndex];
        if (targetFenceValue == 0)
            return;

        if (_fence.GetCompletedValue() < targetFenceValue)
        {
            unsafe
            {
                _fence.SetEventOnCompletion(targetFenceValue, (void*)_fenceEvent);
                WaitForSingleObject(_fenceEvent, uint.MaxValue);
            }
        }
    }

    public void WaitForGpuIdle()
    {
        var finalFenceValue = _fenceValue;
        _commandQueue.Signal(_fence, finalFenceValue);

        if (_fence.GetCompletedValue() < finalFenceValue)
        {
            unsafe
            {
                _fence.SetEventOnCompletion(finalFenceValue, (void*)_fenceEvent);
                WaitForSingleObject(_fenceEvent, uint.MaxValue);
            }
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr CreateEvent(IntPtr lpEventAttributes, bool bManualReset, bool bInitialState, string? lpName);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    public void Initialize(ComPtr<ID3D12Device> device)
    {
        var queueDesc = new CommandQueueDesc
        {
            Type = CommandListType.Direct,
            Flags = CommandQueueFlags.None
        };

        var result = device.CreateCommandQueue(in queueDesc, out _commandQueue);
        if (result < 0)
            throw new Exception($"Failed to create command queue. HRESULT: {result:X8}");

        result = device.CreateFence(0, FenceFlags.None, out _fence);
        if (result < 0)
            throw new Exception($"Failed to create fence. HRESULT: {result:X8}");

        _fenceEvent = CreateEvent(IntPtr.Zero, false, false, null);
        if (_fenceEvent == IntPtr.Zero)
            throw new Exception("Failed to create fence event");

        for (int i = 0; i < FRAME_COUNT; i++)
            _frameFenceValues[i] = 0;

        for (int i = 0; i < FRAME_COUNT; i++)
        {
            result = device.CreateCommandAllocator(CommandListType.Direct, out _commandAllocators[i]);
            if (result < 0)
                throw new Exception($"Failed to create command allocator {i}. HRESULT: {result:X8}");
        }

        result = device.CreateCommandList(
            0,
            CommandListType.Direct,
            _commandAllocators[0],
            new ComPtr<ID3D12PipelineState>(),
            out _commandList);

        if (result < 0)
            throw new Exception($"Failed to create command list. HRESULT: {result:X8}");

        _commandList.Close();
        IsCommandListOpen = false;
    }
}