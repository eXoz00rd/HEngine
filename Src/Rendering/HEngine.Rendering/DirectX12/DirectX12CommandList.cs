using System.Numerics;
using HEngine.Core.Rendering.Contracts;
using Microsoft.Extensions.Logging;

namespace HEngine.Rendering.DirectX12;

public class DirectX12CommandList : IRenderCommandList
{
    private readonly ICommandQueue _commandQueue;
    private readonly ILogger<DirectX12CommandList> _logger;
    private bool _disposed;
    private bool _isReady;

    public DirectX12CommandList(
        ICommandQueue commandQueue,
        ILogger<DirectX12CommandList> logger)
    {
        _commandQueue = commandQueue ?? throw new ArgumentNullException(nameof(commandQueue));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void SetViewMatrix(Matrix4x4 viewMatrix)
    {
        if (_disposed || !_isReady)
        {
            _logger.LogWarning("Cannot set view matrix - CommandList is not ready or disposed");
            return;
        }

        // TODO: Implement view matrix setting
        // This would typically involve updating constant buffers
        _logger.LogDebug("View matrix updated");
    }

    public void SetProjectionMatrix(Matrix4x4 projectionMatrix)
    {
        if (_disposed || !_isReady)
        {
            _logger.LogWarning("Cannot set projection matrix - CommandList is not ready or disposed");
            return;
        }

        // TODO: Implement projection matrix setting
        // This would typically involve updating constant buffers
        _logger.LogDebug("Projection matrix updated");
    }

    public void Reset()
    {
        if (_disposed)
        {
            _logger.LogWarning("Cannot reset disposed CommandList");
            return;
        }

        if (!_commandQueue.IsFrameInProgress)
        {
            _logger.LogWarning("CommandList Reset: CommandQueue has no frame in progress");
            _isReady = false;
            return;
        }

        if (!_commandQueue.IsCommandListOpen)
        {
            _logger.LogWarning("CommandList Reset: Command list is not open");
            _isReady = false;
            return;
        }

        _isReady = true;
        _logger.LogDebug("CommandList Reset: Ready for commands");
    }

    public void Close()
    {
        if (_disposed)
        {
            _logger.LogWarning("Cannot close disposed CommandList");
            return;
        }

        if (!_isReady)
        {
            _logger.LogDebug("CommandList Close: Not ready - nothing to close");
            return;
        }

        _isReady = false;
        _logger.LogDebug("CommandList Close: Marked as not ready");
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _logger.LogDebug("Disposing DirectX12CommandList");
        _isReady = false;
        _disposed = true;
    }
}