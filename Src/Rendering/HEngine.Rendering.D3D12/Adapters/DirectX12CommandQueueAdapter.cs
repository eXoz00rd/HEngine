using HEngine.Core.Rendering.Contracts;
using HEngine.Rendering.DirectX12;

namespace HEngine.Rendering.Adapters;

public class DirectX12CommandQueueAdapter : ICommandQueue
{
    private readonly DirectX12CommandQueue _commandQueue;
    private bool _disposed;

    public DirectX12CommandQueueAdapter(DirectX12CommandQueue commandQueue)
    {
        _commandQueue = commandQueue ?? throw new ArgumentNullException(nameof(commandQueue));
    }

    public bool IsFrameInProgress => _commandQueue.IsFrameInProgress;
    public bool IsCommandListOpen => _commandQueue.IsCommandListOpen;

    public void BeginFrame()
    {
        _commandQueue.BeginFrame();
    }

    public void EndFrame()
    {
        _commandQueue.EndFrame();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _commandQueue?.Dispose();
        _disposed = true;
    }
}