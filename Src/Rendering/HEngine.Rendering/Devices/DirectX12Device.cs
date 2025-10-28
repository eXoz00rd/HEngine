using System.Numerics;
using HEngine.Core.Rendering.Contracts;
using HEngine.Rendering.DirectX12;
using HEngine.Rendering.Input;
using Microsoft.Extensions.Logging;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace HEngine.Rendering.Devices;

public class DirectX12Device : IGraphicsDevice
{
    private const string DeviceIsNotReady = "Device not initialized";
    private readonly DirectX12CommandQueue _commandQueue = new();
    private readonly DirectX12Core _core = new();
    private readonly InputState _inputState;

    private readonly ILogger<DirectX12Device> _logger;
    private readonly DirectX12SwapChain _swapChain = new();

    private bool _disposed;

    private int _frameIndex;
    private bool _initialized;

    private IInputContext _inputContext = null!;
    private IWindow _window = null!;

    public DirectX12Device(InputState inputState, ILogger<DirectX12Device> logger)
    {
        _inputState = inputState ?? throw new ArgumentNullException(nameof(inputState));
        _logger = logger;
    }

    public bool IsInitialized => _initialized && !_disposed;
    public bool ShouldClose => _disposed || _window?.IsClosing == true;

    public void Initialize(int width, int height, string title)
    {
        try
        {
            _window = CreateWindow(width, height, title);
            _window.Initialize();

            _inputContext = _window.CreateInput();
            foreach (var kb in _inputContext.Keyboards)
            {
                kb.KeyDown += OnKeyDown;
                kb.KeyUp += OnKeyUp;
            }

            foreach (var mouse in _inputContext.Mice)
            {
                mouse.MouseMove += _inputState.OnMouseMove;
            }

            _core.Initialize();
            _commandQueue.Initialize(_core.Device);
            _swapChain.Initialize(_core.Device, _commandQueue, _window);

            _frameIndex = (int)_swapChain.GetCurrentBackBufferIndex();
            _initialized = true;
        }
        catch (Exception ex)
        {
            _initialized = false;
            throw;
        }
    }

    public void BeginFrame()
    {
        if (_disposed || !_initialized)
        {
            return;
        }

        _window.DoEvents();

        if (_window.IsClosing)
        {
            return;
        }

        try
        {
            _commandQueue.BeginFrame(_frameIndex);
            var commandList = _commandQueue.CommandList;
            _swapChain.BeginFrame(commandList, _frameIndex);
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex, "Error in BeginFrame");
            }

            throw;
        }
    }

    public void Clear(Vector4 clearColor)
    {
        if (_disposed || !_initialized)
        {
            return;
        }

        var commandList = _commandQueue.CommandList;
        _swapChain.Clear(commandList, clearColor, _frameIndex);
    }

    public void EndFrame()
    {
        if (_disposed || !_initialized)
        {
            return;
        }

        try
        {
            var commandList = _commandQueue.CommandList;
            _swapChain.EndFrame(commandList, _frameIndex);
            _commandQueue.EndFrame(_frameIndex);
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex, "Error in EndFrame");
            }

            throw;
        }
    }

    public void Present()
    {
        if (_disposed || !_initialized)
        {
            return;
        }

        try
        {
            _swapChain.Present();
            MoveToNextFrame();
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex, "Error in Present");
            }

            throw;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Disposing DirectX12Device");
        }

        _initialized = false;

        _swapChain?.Dispose();
        _commandQueue?.Dispose();
        _core?.Dispose();
        _window?.Dispose();

        _disposed = true;
    }

    public ICommandQueue GetCommandQueue()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException();
        }

        return _commandQueue;
    }

    public Vector2 GetWindowSize()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException(DeviceIsNotReady);
        }

        return new Vector2(_window.Size.X, _window.Size.Y);
    }

    private void OnKeyDown(IKeyboard keyboard, Key key, int code)
    {
        _inputState.OnKeyDown(key);
    }

    private void OnKeyUp(IKeyboard keyboard, Key key, int code)
    {
        _inputState.OnKeyUp(key);
    }

    public int GetCurrentFrameIndex()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException(DeviceIsNotReady);
        }

        return _frameIndex;
    }

    private void MoveToNextFrame()
    {
        _frameIndex = (int)_swapChain.GetCurrentBackBufferIndex();
    }

    public ComPtr<ID3D12Device> GetDevice()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException(DeviceIsNotReady);
        }

        return _core.Device;
    }

    public DirectX12CommandQueue GetDirectX12CommandQueue()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException(DeviceIsNotReady);
        }

        return _commandQueue;
    }

    private IWindow CreateWindow(int width, int height, string title)
    {
        var options = WindowOptions.Default;
        options.Size = new Vector2D<int>(width, height);
        options.Title = title;
        options.API = GraphicsAPI.None;
        options.VSync =
            false;

        var window = Window.Create(options);

        window.Load += () =>
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Window loaded");
            }
        };
        window.Closing += () =>
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Window closing");
            }

            Stop();
        };

        return window;
    }

    private void Stop()
    {
        _initialized = false;

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("DirectX12Device stopped");
        }
    }
}