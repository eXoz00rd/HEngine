using System.Numerics;
using System.Runtime.InteropServices;
using HEngine.Core.Rendering.Contracts;
using HEngine.Rendering.DirectX12;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace HEngine.Rendering.Devices;

public class DirectX12Device : IGraphicsDevice
{
    private const int FrameCount = 2; // Musi być spójne z DirectX12SwapChain
    private readonly DirectX12CommandQueue _commandQueue = new();
    private readonly DirectX12Core _core = new();
    private readonly ulong[] _frameFenceValues = new ulong[FrameCount];
    private readonly DirectX12SwapChain _swapChain = new();

    private bool _disposed;

    // --- Zasoby do synchronizacji ---
    private ComPtr<ID3D12Fence> _fence;
    private IntPtr _fenceEvent;
    private ulong _fenceValue;
    private int _frameIndex;
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

            // Inicjalizacja zasobów synchronizacji (Fence)
            var hresult = _core.Device.CreateFence(0, FenceFlags.None, out _fence);
            if (hresult < 0)
                throw new Exception($"Failed to create fence. HRESULT: {hresult:X8}");

            _fenceValue = 1; // Wartości Fence zaczynają się od 1

            _fenceEvent = Kernel32.CreateEvent(IntPtr.Zero, false, false, null);
            if (_fenceEvent == IntPtr.Zero)
                throw new Exception("Failed to create fence event.");

            _frameIndex = (int)_swapChain.GetCurrentBackBufferIndex();
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
            _commandQueue.BeginFrame();
            var commandList = _commandQueue.CommandList;
            _swapChain.BeginFrame(commandList, _frameIndex);
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
        _swapChain.Clear(commandList, clearColor, _frameIndex);
    }

    public void EndFrame()
    {
        if (_disposed || !_initialized)
            return;

        try
        {
            var commandList = _commandQueue.CommandList;
            _swapChain.EndFrame(commandList, _frameIndex);
            _commandQueue.EndFrame();
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
            MoveToNextFrame(); // Synchronizacja CPU-GPU
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in Present: {ex.Message}");
            throw;
        }
    }

    public Vector2 GetWindowSize()
    {
        if (!_initialized)
            throw new InvalidOperationException("Device not initialized");
        return new Vector2(_window.Size.X, _window.Size.Y);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        Console.WriteLine("Disposing DirectX12Device");
        _initialized = false;

        WaitForGpuIdle(); // Czekaj, aż GPU zakończy całą pracę

        _swapChain?.Dispose();
        _commandQueue?.Dispose();
        _core?.Dispose();
        _window?.Dispose();

        _fence.Dispose();
        Kernel32.CloseHandle(_fenceEvent);

        _disposed = true;
    }

    public ICommandQueue GetCommandQueue()
    {
        if (!_initialized)
            throw new InvalidOperationException("Device not initialized");
        return _commandQueue;
    }

    private unsafe void MoveToNextFrame()
    {
        // Zaplanuj sygnał na CommandQueue, który zostanie wykonany, gdy GPU dojdzie do tego punktu.
        var currentFenceValue = _fenceValue;
        _commandQueue.Queue.Signal(_fence, currentFenceValue);

        // Zapisz wartość Fence dla bieżącej klatki, która właśnie została wysłana do GPU.
        _frameFenceValues[_frameIndex] = currentFenceValue;

        // Przejdź do następnego indeksu klatki.
        _frameIndex = (int)_swapChain.GetCurrentBackBufferIndex();

        // Sprawdź, czy praca nad klatką, którą zamierzamy teraz renderować, została już ukończona przez GPU.
        if (_fence.GetCompletedValue() < _frameFenceValues[_frameIndex])
        {
            // Jeśli nie, to powiedz GPU, aby zasygnalizowało nasz event, gdy osiągnie wymaganą wartość Fence.
            _fence.SetEventOnCompletion(_frameFenceValues[_frameIndex], (void*)_fenceEvent);
            // Czekaj na ten event. To uśpi wątek CPU do czasu, aż GPU będzie gotowe.
            Kernel32.WaitForSingleObject(_fenceEvent, uint.MaxValue); // Czekaj w nieskończoność
        }

        // Zwiększ wartość Fence na następną klatkę.
        _fenceValue++;
    }

    private unsafe void WaitForGpuIdle()
    {
        // Wyślij sygnał na sam koniec kolejki poleceń.
        _commandQueue.Queue.Signal(_fence, _fenceValue);

        // Czekaj, aż GPU osiągnie ten punkt.
        if (_fence.GetCompletedValue() < _fenceValue)
        {
            _fence.SetEventOnCompletion(_fenceValue, (void*)_fenceEvent);
            Kernel32.WaitForSingleObject(_fenceEvent, uint.MaxValue);
        }

        _fenceValue++;
    }

    public ComPtr<ID3D12Device> GetDevice()
    {
        if (!_initialized)
            throw new InvalidOperationException("Device not initialized");
        return _core.Device;
    }

    public DirectX12CommandQueue GetDirectX12CommandQueue()
    {
        if (!_initialized)
            throw new InvalidOperationException("Device not initialized");
        return _commandQueue;
    }

    private IWindow CreateWindow(int width, int height, string title)
    {
        var options = WindowOptions.Default;
        options.Size = new Vector2D<int>(width, height);
        options.Title = title;
        options.API = GraphicsAPI.None;
        options.VSync =
            false; // VSync jest obsługiwany przez SwapChain.Present. Wyłączenie go tutaj zapobiega podwójnemu ograniczaniu.

        var window = Window.Create(options);

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
}

// Klasa pomocnicza do obsługi eventów systemowych (umieszczona tutaj dla wygody).
internal static class Kernel32
{
    [DllImport("kernel32.dll")]
    internal static extern IntPtr CreateEvent(IntPtr lpEventAttributes, bool bManualReset, bool bInitialState,
        string? lpName);

    [DllImport("kernel32.dll")]
    internal static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll")]
    internal static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);
}