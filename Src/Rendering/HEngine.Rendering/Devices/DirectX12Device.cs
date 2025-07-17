using System.Numerics;
using HEngine.Rendering.Contracts;
using HEngine.Rendering.DirectX12;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace HEngine.Rendering.Devices;

public class DirectX12Device : IRenderDevice
{
    private readonly DirectX12CommandQueue _commandQueue = new();
    private readonly DirectX12Core _core = new();
    private readonly DirectX12SwapChain _swapChain = new();
    private bool _disposed;
    private bool _initialized;
    private IWindow _window = null!;
    public bool IsInitialized => _initialized && !_disposed;

    public bool ShouldClose => _disposed || _window?.IsClosing == true;

    public void Initialize(int width, int height, string title)
    {
        try
        {
            Console.WriteLine("Creating window...");
            _window = CreateWindow(width, height, title);

            _window.Initialize();

            Console.WriteLine("Window created, initializing DirectX...");

            _core.Initialize();
            _commandQueue.Initialize(_core.Device);
            _swapChain.Initialize(_core.Device, _commandQueue, _window);

            _initialized = true;
            Console.WriteLine("DirectX12Device initialized successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize DirectX12Device: {ex.Message}");
            _initialized = false;
            throw;
        }
    }


    public void BeginFrame()
    {
        if (_disposed || !_initialized)
            return;

        _window.DoEvents();

        if (_window.IsClosing)
            return;

        try
        {
            Console.WriteLine("DirectX12Device BeginFrame starting...");

            _commandQueue.BeginFrame();
            var commandList = _commandQueue.CommandList;
            _swapChain.BeginFrame(commandList);

            Console.WriteLine("DirectX12Device BeginFrame completed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in BeginFrame: {ex.Message}");
            throw;
        }
    }

    public void Clear(Vector4 clearColor)
    {
        if (_disposed || !_initialized)
            return;

        var commandList = _commandQueue.CommandList;
        _swapChain.Clear(commandList, clearColor);
    }

    public void EndFrame()
    {
        if (_disposed || !_initialized)
            return;

        try
        {
            Console.WriteLine("DirectX12Device EndFrame starting...");

            var commandList = _commandQueue.CommandList;
            _swapChain.EndFrame(commandList);
            _commandQueue.EndFrame();

            Console.WriteLine("DirectX12Device EndFrame completed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in EndFrame: {ex.Message}");
            throw;
        }
    }

    public void Present()
    {
        if (_disposed || !_initialized)
            return;

        try
        {
            _swapChain.Present();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in Present: {ex.Message}");
            throw;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        Console.WriteLine("Disposing DirectX12Device");
        _initialized = false;

        _swapChain?.Dispose();
        _commandQueue?.Dispose();
        _core?.Dispose();
        _window?.Dispose();
        _disposed = true;
    }

    public ComPtr<ID3D12Device> GetDevice()
    {
        return !_initialized ? throw new InvalidOperationException("Device not initialized") : _core.Device;
    }

    private IWindow CreateWindow(int width, int height, string title)
    {
        var options = WindowOptions.Default;
        options.Size = new Vector2D<int>(width, height);
        options.Title = title;
        options.API = GraphicsAPI.None; // Nie używamy OpenGL/Vulkan
        options.VSync = true;

        var window = Window.Create(options);

        // Dodaj obsługę zdarzeń okna
        window.Load += () => { Console.WriteLine("Window loaded"); };

        window.Closing += () =>
        {
            Console.WriteLine("Window closing");
            Stop();
        };

        return window;
    }

    private void Stop()
    {
        _initialized = false;
        Console.WriteLine("DirectX12Device stopped");
    }

    public DirectX12CommandQueue GetCommandQueue()
    {
        return !_initialized ? throw new InvalidOperationException("Device not initialized") : _commandQueue;
    }
}