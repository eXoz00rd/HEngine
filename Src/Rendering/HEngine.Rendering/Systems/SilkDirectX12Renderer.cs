using System.Numerics;
using HEngine.Rendering.Batches;
using HEngine.Rendering.Contracts;
using HEngine.Rendering.Devices;
using HEngine.Rendering.DirectX12;
using HEngine.Rendering.Renderers;

namespace HEngine.Rendering.Systems;

public class SilkDirectX12Renderer : IRenderer
{
    private readonly DirectX12Device _device;
    private readonly IRenderBatch<SpriteData> _spriteBatch;
    private readonly DirectX12SpriteRenderer _spriteRenderer;
    private IRenderCommandList _commandList;
    private bool _disposed;
    private bool _frameInProgress;
    private bool _initialized;

    public SilkDirectX12Renderer()
    {
        _device = new DirectX12Device();
        _commandList = null!;
        _spriteRenderer = new DirectX12SpriteRenderer();
        _spriteBatch = new SpriteBatch();
    }

    public Action<float>? OnFrameUpdate { get; set; }
    public bool IsInitialized => _initialized && _device.IsInitialized;
    public bool ShouldClose => _device.ShouldClose;

    public void Initialize(int width, int height, string title)
    {
        try
        {
            Console.WriteLine("Initializing SilkDirectX12Renderer...");

            _device.Initialize(width, height, title);

            if (!_device.IsInitialized)
                throw new InvalidOperationException("DirectX12Device failed to initialize");

            var commandQueue = _device.GetCommandQueue();
            _commandList = new DirectX12CommandList(commandQueue);

            _spriteRenderer.Initialize(_device);
            ((SpriteBatch)_spriteBatch).Initialize(_spriteRenderer);

            _initialized = true;
            Console.WriteLine("SilkDirectX12Renderer initialized successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize SilkDirectX12Renderer: {ex.Message}");
            _initialized = false;
            throw;
        }
    }

    public void Run()
    {
        // Ta metoda nie jest używana w tym przypadku
        // Pętla renderowania jest zarządzana przez GameEngine
    }

    public void Present()
    {
        if (_disposed || !IsInitialized)
            return;

        try
        {
            _device.Present();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in Present: {ex.Message}");
            throw;
        }
    }

    public void PollEvents()
    {
        if (_disposed || !IsInitialized)
            return;

        // Events są obsługiwane w _device.BeginFrame()
    }

    public void BeginFrame()
    {
        if (_disposed || !IsInitialized)
            return;

        try
        {
            if (_frameInProgress)
            {
                Console.WriteLine("Previous frame still in progress - skipping BeginFrame");
                return;
            }

            _device.BeginFrame();

            if (_device.ShouldClose)
                return;

            var commandQueue = _device.GetCommandQueue();
            if (!commandQueue.IsFrameInProgress)
            {
                Console.WriteLine("DirectX12Device failed to start frame - skipping");
                return;
            }

            _commandList.Reset();
            _spriteBatch.Clear();
            _frameInProgress = true;

            Console.WriteLine("SilkDirectX12Renderer BeginFrame completed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to begin frame: {ex.Message}");
            _frameInProgress = false;
            throw;
        }
    }

    public void EndFrame()
    {
        if (_disposed || !IsInitialized)
            return;

        try
        {
            if (_device.ShouldClose)
                return;

            if (!_frameInProgress)
            {
                Console.WriteLine("No frame in progress - skipping EndFrame");
                return;
            }

            // Renderuj sprite batch
            _spriteBatch.Render(_commandList);

            // Flush sprite renderer
            _spriteRenderer.FlushBatch();

            _commandList.Close();
            _device.EndFrame();
            _frameInProgress = false;

            Console.WriteLine("SilkDirectX12Renderer EndFrame completed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to end frame: {ex.Message}");
            _frameInProgress = false;
            throw;
        }
    }

    public void Clear(Vector4 clearColor)
    {
        if (_disposed || !IsInitialized)
            return;
        _device.Clear(clearColor);
    }

    public void SetViewMatrix(Matrix4x4 viewMatrix)
    {
        if (_disposed || !IsInitialized)
            return;
        _commandList.SetViewMatrix(viewMatrix);
    }

    public void SetProjectionMatrix(Matrix4x4 projectionMatrix)
    {
        if (_disposed || !IsInitialized)
            return;
        _commandList.SetProjectionMatrix(projectionMatrix);
    }

    public void DrawSprite(Vector2 position, Vector2 size, Vector4 color)
    {
        if (_disposed || !IsInitialized)
            return;

        _spriteBatch.Add(new SpriteData
        {
            Position = position,
            Size = size,
            Color = color
        });
        
        _spriteRenderer.DrawSprite(position, size, color);
    }

    public void DrawMesh(Matrix4x4 transform, ReadOnlySpan<float> vertices, ReadOnlySpan<uint> indices)
    {
        // TODO: Implement mesh rendering through composition
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        Console.WriteLine("Disposing SilkDirectX12Renderer");
        _initialized = false;

        _spriteBatch?.Dispose();
        _spriteRenderer?.Dispose();
        _commandList?.Dispose();
        _device?.Dispose();
        _disposed = true;
    }
}