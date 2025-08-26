using HEngine.Core.Rendering.Contracts;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;

namespace HEngine.Rendering.DirectX12;

public class DirectX12CommandQueue : ICommandQueue
{
    private readonly D3D12 _d3d12 = D3D12.GetApi();
    private ComPtr<ID3D12CommandAllocator> _commandAllocator;
    private ComPtr<ID3D12GraphicsCommandList> _commandList;
    private ComPtr<ID3D12CommandQueue> _commandQueue;
    private bool _disposed;

    // DirectX12-specific properties for internal use
    public ComPtr<ID3D12CommandQueue> Queue => _commandQueue;
    public ComPtr<ID3D12GraphicsCommandList> CommandList => _commandList;

    public bool IsFrameInProgress { get; private set; }
    public bool IsCommandListOpen { get; private set; }

    public void BeginFrame()
    {
        if (IsFrameInProgress)
            throw new InvalidOperationException("Frame already in progress");

        _commandAllocator.Reset();

        // Explicitly specify the pipeline state parameter as null pointer
        unsafe
        {
            _commandList.Reset(_commandAllocator, (ID3D12PipelineState*)null);
        }

        IsCommandListOpen = true;
        IsFrameInProgress = true;
    }

    public void EndFrame()
    {
        if (!IsFrameInProgress)
            throw new InvalidOperationException("No frame in progress");

        if (IsCommandListOpen)
        {
            _commandList.Close();
            IsCommandListOpen = false;
        }

        // Execute command list
        unsafe
        {
            var commandLists = stackalloc ID3D12CommandList*[1];
            commandLists[0] = (ID3D12CommandList*)_commandList.Handle;
            _commandQueue.ExecuteCommandLists(1, commandLists);
        }

        IsFrameInProgress = false;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _commandList.Dispose();
        _commandAllocator.Dispose();
        _commandQueue.Dispose();
        _d3d12.Dispose();
        _disposed = true;
    }

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

        result = device.CreateCommandAllocator(CommandListType.Direct, out _commandAllocator);
        if (result < 0)
            throw new Exception($"Failed to create command allocator. HRESULT: {result:X8}");

        result = device.CreateCommandList(
            0,
            CommandListType.Direct,
            _commandAllocator,
            new ComPtr<ID3D12PipelineState>(),
            out _commandList);

        if (result < 0)
            throw new Exception($"Failed to create command list. HRESULT: {result:X8}");

        _commandList.Close();
        IsCommandListOpen = false;
    }
}