using System.Runtime.InteropServices;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;

namespace HEngine.Rendering.DirectX12;

public class DirectX12CommandQueue : IDisposable
{
    private ComPtr<ID3D12CommandAllocator> _commandAllocator;
    private ComPtr<ID3D12GraphicsCommandList> _commandList;
    private ComPtr<ID3D12CommandQueue> _commandQueue;
    private bool _disposed;
    private ComPtr<ID3D12Fence> _fence;
    private ulong _fenceValue;

    public ComPtr<ID3D12CommandQueue> Queue => _commandQueue;
    public ComPtr<ID3D12GraphicsCommandList> CommandList => _commandList;
    public bool IsFrameInProgress { get; private set; }

    public bool IsCommandListOpen { get; private set; }

    public void Dispose()
    {
        if (_disposed)
            return;

        // Upewnij się, że frame jest zakończony
        if (IsFrameInProgress)
            try
            {
                EndFrame();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error ending frame during dispose: {ex.Message}");
            }

        WaitForGpu();

        _commandList.Dispose();
        _commandAllocator.Dispose();
        _commandQueue.Dispose();
        _fence.Dispose();
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

        ComPtr<ID3D12PipelineState> nullPipelineState = default;
        result = device.CreateCommandList(
            0,
            CommandListType.Direct,
            _commandAllocator,
            nullPipelineState,
            out _commandList
        );
        if (result < 0)
            throw new Exception($"Failed to create command list. HRESULT: {result:X8}");

        // Zamknij command list po utworzeniu
        result = _commandList.Close();
        if (result < 0)
            throw new Exception($"Failed to close initial command list. HRESULT: {result:X8}");

        IsCommandListOpen = false;

        result = device.CreateFence(0, FenceFlags.None, out _fence);
        if (result < 0)
            throw new Exception($"Failed to create fence. HRESULT: {result:X8}");

        // Poprawka: Zainicjalizuj fence value na 0, żeby pierwsza ramka mogła się uruchomić
        _fenceValue = 0;

        Console.WriteLine("DirectX12CommandQueue initialized successfully");
    }


    public void BeginFrame()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(DirectX12CommandQueue));

        if (IsFrameInProgress)
        {
            Console.WriteLine("DirectX12CommandQueue: Frame already in progress - will try to continue");
            // Nie zwracaj - spróbuj kontynuować jeśli GPU jest gotowy
            if (IsCommandListOpen)
            {
                Console.WriteLine("DirectX12CommandQueue: Command list already open - skipping BeginFrame");
                return;
            }
        }

        unsafe
        {
            if (_commandAllocator.Handle == (void*)IntPtr.Zero)
                throw new InvalidOperationException("Command allocator is null or disposed");

            if (_commandList.Handle == (void*)IntPtr.Zero)
                throw new InvalidOperationException("Command list is null or disposed");
        }

        if (!IsGpuReady())
        {
            Console.WriteLine("DirectX12CommandQueue: GPU not ready - skipping frame");
            return;
        }

        try
        {
            Console.WriteLine("DirectX12CommandQueue: Starting frame...");

            var result = _commandAllocator.Reset();
            if (result < 0)
                throw new Exception($"Failed to reset command allocator. HRESULT: {result:X8}");

            ComPtr<ID3D12PipelineState> nullPipelineState = default;
            result = _commandList.Reset(_commandAllocator, nullPipelineState);
            if (result < 0)
                throw new Exception($"Failed to reset command list. HRESULT: {result:X8}");

            IsFrameInProgress = true;
            IsCommandListOpen = true;

            Console.WriteLine($"DirectX12CommandQueue: Frame started successfully. Fence value: {_fenceValue}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DirectX12CommandQueue: Error in BeginFrame: {ex.Message}");
            IsFrameInProgress = false;
            IsCommandListOpen = false;
            throw;
        }
    }

    public void EndFrame()
    {
        if (_disposed)
            return;

        if (!IsFrameInProgress)
        {
            Console.WriteLine("DirectX12CommandQueue: No frame in progress - skipping EndFrame");
            return;
        }

        unsafe
        {
            if (_commandList.Handle == (void*)IntPtr.Zero)
            {
                Console.WriteLine("DirectX12CommandQueue: Command list is null - skipping EndFrame");
                IsFrameInProgress = false;
                IsCommandListOpen = false;
                return;
            }

            try
            {
                Console.WriteLine("DirectX12CommandQueue: Ending frame...");

                // Zamknij command list tylko jeśli jest otwarty
                if (IsCommandListOpen)
                {
                    var result = _commandList.Close();
                    if (result < 0)
                        throw new Exception($"Failed to close command list. HRESULT: {result:X8}");

                    IsCommandListOpen = false;
                }

                // Wykonaj command list
                var commandLists = stackalloc ID3D12CommandList*[1];
                commandLists[0] = (ID3D12CommandList*)_commandList.Handle;
                _commandQueue.ExecuteCommandLists(1, commandLists);

                // Zwiększ fence value PRZED sygnalizowaniem
                _fenceValue++;

                // Sygnalizuj fence
                var signalResult = _commandQueue.Signal(_fence, _fenceValue);
                if (signalResult < 0)
                    throw new Exception($"Failed to signal fence. HRESULT: {signalResult:X8}");

                IsFrameInProgress = false;

                Console.WriteLine($"DirectX12CommandQueue: Frame ended successfully. Fence value: {_fenceValue}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DirectX12CommandQueue: Error in EndFrame: {ex.Message}");
                IsFrameInProgress = false;
                IsCommandListOpen = false;
                throw;
            }
        }
    }

    private bool IsGpuReady()
    {
        unsafe
        {
            if (_disposed || _fence.Handle == (void*)IntPtr.Zero)
                return false;

            var completedValue = _fence.GetCompletedValue();

            // Dla pierwszej ramki (gdy _fenceValue == 0) lub gdy GPU zakończył poprzednią ramkę
            var isReady = completedValue >= _fenceValue;

            if (!isReady)
                // Zmniejsz szczegółowość logowania - zbyt wiele komunikatów
                if (_fenceValue % 10 == 0) // Log co 10 ramek
                    Console.WriteLine($"GPU not ready. Completed: {completedValue}, Expected: {_fenceValue}");

            return isReady;
        }
    }

    private void WaitForGpu()
    {
        unsafe
        {
            if (_disposed || _fence.Handle == (void*)IntPtr.Zero)
                return;

            var completedValue = _fence.GetCompletedValue();
            if (completedValue < _fenceValue)
            {
                var eventHandle = CreateEventW(IntPtr.Zero, false, false, IntPtr.Zero);
                if (eventHandle == IntPtr.Zero)
                    throw new Exception("Failed to create event handle");

                var result = _fence.SetEventOnCompletion(_fenceValue, (void*)eventHandle);
                if (result < 0)
                {
                    CloseHandle(eventHandle);
                    throw new Exception($"Failed to set event on completion. HRESULT: {result:X8}");
                }

                // Zwiększ timeout dla dispose
                var waitResult = WaitForSingleObject(eventHandle, 10000); // 10 sekund
                CloseHandle(eventHandle);

                if (waitResult == 0x00000102) // WAIT_TIMEOUT
                    Console.WriteLine("GPU sync timeout in dispose - forcing cleanup");
                else if (waitResult != 0) // WAIT_OBJECT_0
                    Console.WriteLine($"GPU sync failed in dispose. Wait result: {waitResult}");
            }
        }
    }

    [DllImport("kernel32.dll")]
    private static extern IntPtr CreateEventW(
        IntPtr lpEventAttributes,
        bool bManualReset,
        bool bInitialState,
        IntPtr lpName);

    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll")]
    private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);
}