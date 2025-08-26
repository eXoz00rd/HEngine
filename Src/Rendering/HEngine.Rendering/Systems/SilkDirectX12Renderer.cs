using System.Numerics;
using HEngine.Core.Rendering.Contracts;
using HEngine.Rendering.Batches;
using HEngine.Rendering.DirectX12;
using Microsoft.Extensions.Logging;

namespace HEngine.Rendering.Systems;

public class SilkDirectX12Renderer : IRenderer
{
    private readonly IGraphicsDevice _device;
    private readonly ILogger<SilkDirectX12Renderer> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IShaderManager _shaderManager;
    private readonly IRenderBatch<SpriteData> _spriteBatch;
    private readonly ISpriteRenderer _spriteRenderer;

    private DirectX12CommandList _commandList;
    private bool _disposed;
    private bool _frameInProgress;
    private bool _initialized;

    public SilkDirectX12Renderer(IGraphicsDevice device, IRenderBatch<SpriteData> spriteBatch,
        ISpriteRenderer spriteRenderer, IShaderManager shaderManager, ILogger<SilkDirectX12Renderer> logger,
        ILoggerFactory loggerFactory)
    {
        _device = device ?? throw new ArgumentNullException(nameof(device));
        _spriteBatch = spriteBatch ?? throw new ArgumentNullException(nameof(spriteBatch));
        _spriteRenderer = spriteRenderer ?? throw new ArgumentNullException(nameof(spriteRenderer));
        _shaderManager = shaderManager ?? throw new ArgumentNullException(nameof(shaderManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _commandList = null!;
    }

    public Action<float>? OnFrameUpdate { get; set; }
    public bool IsInitialized => _initialized && _device.IsInitialized;
    public bool ShouldClose => _device.ShouldClose;

    public void Initialize(int width, int height, string title)
    {
        try
        {
            _logger.LogInformation("Initializing SilkDirectX12Renderer...");

            _device.Initialize(width, height, title);

            if (!_device.IsInitialized)
            {
                var errorMessage = "GraphicsDevice failed to initialize";
                _logger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            _shaderManager.Initialize();

            var commandQueue = _device.GetCommandQueue();

            // Bezpośrednie tworzenie DirectX12CommandList
            var commandListLogger = _loggerFactory.CreateLogger<DirectX12CommandList>();
            _commandList = new DirectX12CommandList(commandQueue, commandListLogger);

            _spriteRenderer.Initialize(_device);
            _spriteBatch.Initialize(_spriteRenderer);

            _initialized = true;
            _logger.LogInformation("SilkDirectX12Renderer initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize SilkDirectX12Renderer");
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
            _logger.LogError(ex, "Error in Present");
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
                _logger.LogWarning("Previous frame still in progress - skipping BeginFrame");
                return;
            }

            _device.BeginFrame();

            if (_device.ShouldClose)
                return;

            var commandQueue = _device.GetCommandQueue();
            if (!commandQueue.IsFrameInProgress)
            {
                _logger.LogWarning("GraphicsDevice failed to start frame - skipping");
                return;
            }

            _commandList.Reset();
            _spriteBatch.Clear();
            _frameInProgress = true;

            // Zmieniono z LogDebug na Console.WriteLine dla spójności
            Console.WriteLine("Renderer: BeginFrame completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to begin frame");
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
                _logger.LogWarning("No frame in progress - skipping EndFrame");
                return;
            }

            _commandList.Close();
            _device.EndFrame();
            _frameInProgress = false;

            // Zmieniono z LogDebug na Console.WriteLine dla spójności
            Console.WriteLine("Renderer: EndFrame completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to end frame");
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
        Console.WriteLine($"Renderer SetViewMatrix: {viewMatrix}");
        _commandList.SetViewMatrix(viewMatrix);
    }

    public void SetProjectionMatrix(Matrix4x4 projectionMatrix)
    {
        if (_disposed || !IsInitialized)
            return;
        Console.WriteLine($"Renderer SetProjectionMatrix: {projectionMatrix}");
        _commandList.SetProjectionMatrix(projectionMatrix);
    }

    public void DrawSprite(Vector2 position, Vector2 size, Vector4 color)
    {
        if (_disposed || !IsInitialized)
            return;

        Console.WriteLine($"Renderer DrawSprite: Pos={position}, Size={size}, Color={color}");
        _spriteBatch.Add(new SpriteData
        {
            Position = position,
            Size = size,
            Color = color
        });
    }

    public void FlushBatch()
    {
        if (_disposed || !IsInitialized) return;

        if (!_frameInProgress)
        {
            Console.WriteLine("Renderer FlushBatch SKIPPED: Frame not in progress.");
            return;
        }

        Console.WriteLine("Renderer: Flushing batch...");
        _spriteBatch.Render(_commandList);
        Console.WriteLine("Renderer: Batch flushed.");
    }

    public void DrawMesh(Matrix4x4 transform, ReadOnlySpan<float> vertices, ReadOnlySpan<uint> indices)
    {
        // TODO: Implement mesh rendering through composition
        _logger.LogWarning("Mesh rendering not yet implemented");
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _logger.LogInformation("Disposing SilkDirectX12Renderer");
        _initialized = false;

        _spriteBatch?.Dispose();
        _spriteRenderer?.Dispose();
        _commandList?.Dispose();
        _shaderManager?.Dispose();
        _device?.Dispose();
        _disposed = true;
    }
}