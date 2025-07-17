using System.Numerics;
using HEngine.Core.Components.Transform;
using HEngine.Core.Configuration;
using HEngine.Core.Managers;
using HEngine.Core.Time;
using HEngine.Rendering.Components;
using HEngine.Rendering.Contracts;
using HEngine.Rendering.Managers;
using HEngine.Rendering.Systems;

namespace HEngine;

public class GameEngine : IDisposable
{
    private readonly GameTime _gameTime;
    private readonly RenderingSystem _renderingSystem;
    private readonly RenderManager _renderManager;
    private readonly SystemManager _systemManager;
    private readonly WorldManager _worldManager;
    private bool _disposed;
    private bool _running;

    public GameEngine() : this(new EngineConfiguration())
    {
    }

    private GameEngine(EngineConfiguration config)
    {
        _gameTime = new GameTime();
        _worldManager = new WorldManager();
        _systemManager = new SystemManager();

        var renderer = CreateRenderer();
        _renderManager = new RenderManager(renderer, config);
        _renderingSystem = CreateRenderingSystem();

        Initialize();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _running = false;
        _systemManager.Dispose();
        _renderManager.Dispose();
        _worldManager.Dispose();
        _disposed = true;
    }

    public void Run()
    {
        try
        {
            _running = true;
            _gameTime.Reset();

            Console.WriteLine("Starting game loop...");

            if (!IsInitialized())
            {
                Console.WriteLine("Engine not properly initialized");
                return;
            }

            var frameCount = 0;
            var successfulFrames = 0;
            var skippedFrames = 0;

            var targetFrameTime = TimeSpan.FromMilliseconds(1000.0 / 60.0);
            var lastFrameTime = DateTime.Now;

            while (_running)
            {
                frameCount++;

                if (_renderManager.ShouldClose)
                {
                    Console.WriteLine("Window should close - stopping game loop");
                    break;
                }

                try
                {
                    _gameTime.Update();

                    if (!_renderManager.CanRender)
                    {
                        if (frameCount % 60 == 0)
                            Console.WriteLine("Cannot render - waiting...");
                        Thread.Sleep(1);
                        continue;
                    }

                    _renderManager.UpdateInput();
                    _systemManager.Update(_gameTime.DeltaTime);

                    _renderManager.BeginRender();
                    _renderingSystem.Update(_gameTime.DeltaTime);
                    _renderManager.EndRender();

                    successfulFrames++;

                    var currentTime = DateTime.Now;
                    var frameTime = currentTime - lastFrameTime;
                    if (frameTime < targetFrameTime) Thread.Sleep(targetFrameTime - frameTime);
                    lastFrameTime = DateTime.Now;

                    if (frameCount % 120 == 0)
                    {
                        Console.WriteLine(
                            $"Frame {frameCount}: FPS: {_gameTime.FPS:F1}, Delta: {_gameTime.DeltaTime * 1000:F1}ms");
                        Console.WriteLine(
                            $"Successful: {successfulFrames}, Skipped: {skippedFrames}, Total: {frameCount}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in game loop (frame {frameCount}): {ex.Message}");
                    Thread.Sleep(10);
                    skippedFrames++;

                    if (frameCount - successfulFrames > 100)
                    {
                        Console.WriteLine("Too many failed frames - stopping engine");
                        break;
                    }
                }
            }

            Console.WriteLine(
                $"Game loop ended. Total: {frameCount}, Successful: {successfulFrames}, Skipped: {skippedFrames}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fatal error in game loop: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    private bool IsInitialized()
    {
        return _renderManager != null && _systemManager != null && _worldManager != null;
    }

    public void Stop()
    {
        _running = false;
    }

    private void Initialize()
    {
        try
        {
            _renderManager.Initialize();

            _renderingSystem.SetRenderContext(_renderManager.RenderContext);
            _renderingSystem.Initialize(_worldManager);

            _systemManager.AddSystem(_renderingSystem);

            CreateExampleEntities();

            Console.WriteLine("Game engine initialized successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize game engine: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    private IRenderer CreateRenderer()
    {
        return new SilkDirectX12Renderer();
    }

    private RenderingSystem CreateRenderingSystem()
    {
        var spriteSystem = new SpriteRenderingSystem();
        var meshSystem = new MeshRenderingSystem();
        return new RenderingSystem(spriteSystem, meshSystem);
    }

    private void CreateExampleEntities()
    {
        var spriteEntity = _worldManager.CreateEntity();

        _worldManager.AddComponent(
            spriteEntity,
            new Transform2D
            {
                Position = new Vector2(100, 100)
            }
        );

        _worldManager.AddComponent(
            spriteEntity,
            new Sprite
            {
                Size = new Vector2(200, 64),
                Color = new Vector4(1, 1, 0, 1),
                Origin = new Vector2(0.5f, 0.5f)
            }
        );
    }
}