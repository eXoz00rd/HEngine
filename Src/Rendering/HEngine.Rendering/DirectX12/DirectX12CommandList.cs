using System.Numerics;
using HEngine.Rendering.Contracts;

namespace HEngine.Rendering.DirectX12;

public class DirectX12CommandList : IRenderCommandList
{
    private readonly DirectX12CommandQueue _commandQueue;
    private bool _disposed;
    private bool _isReady;

    public DirectX12CommandList(DirectX12CommandQueue commandQueue)
    {
        _commandQueue = commandQueue ?? throw new ArgumentNullException(nameof(commandQueue));
    }

    public void SetViewMatrix(Matrix4x4 viewMatrix)
    {
        if (_disposed || !_isReady)
            return;

        // TODO: Implement view matrix settingf
        // This would typically involve updating constant buffers
    }

    public void SetProjectionMatrix(Matrix4x4 projectionMatrix)
    {
        if (_disposed || !_isReady)
            return;

        // TODO: Implement projection matrix setting
        // This would typically involve updating constant buffers
    }

    public void Reset()
    {
        if (_disposed)
            return;

        // Sprawdź czy CommandQueue faktycznie ma ramkę w toku
        if (!_commandQueue.IsFrameInProgress)
        {
            Console.WriteLine("CommandList Reset: CommandQueue has no frame in progress");
            _isReady = false;
            return;
        }

        // Sprawdź czy command list jest otwarty
        if (!_commandQueue.IsCommandListOpen)
        {
            Console.WriteLine("CommandList Reset: Command list is not open");
            _isReady = false;
            return;
        }

        _isReady = true;
        Console.WriteLine("CommandList Reset: Ready for commands");
    }

    public void Close()
    {
        if (_disposed)
            return;

        if (!_isReady)
        {
            Console.WriteLine("CommandList Close: Not ready - nothing to close");
            return;
        }

        // Nie sprawdzaj _commandQueue.IsFrameInProgress tutaj - to może się zmienić
        _isReady = false;
        Console.WriteLine("CommandList Close: Marked as not ready");
    }


    public void Dispose()
    {
        if (_disposed)
            return;

        _isReady = false;
        _disposed = true;

        Console.WriteLine("CommandList disposed");
    }
}