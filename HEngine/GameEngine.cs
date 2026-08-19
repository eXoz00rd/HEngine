using System.Numerics;
using HEngine.Builders;
using HEngine.Core.Components.Rendering;
using HEngine.Core.Configuration;
using HEngine.Core.Contracts;
using HEngine.Core.Managers;
using HEngine.Core.Primitives;
using HEngine.Core.Rendering.Contracts;
using HEngine.Core.Systems;
using HEngine.Rendering.Managers;
using HEngine.Rendering.PostProcessing;
using Microsoft.Extensions.Logging;

namespace HEngine;

file sealed class CameraAdapter : ICamera
{
    private readonly WorldManager _world;
    private readonly Entity _cameraEntity;

    public CameraAdapter(WorldManager world, Entity cameraEntity)
    {
        _world = world;
        _cameraEntity = cameraEntity;
    }

    public Matrix4x4 ViewMatrix
    {
        get
        {
            if (_world.HasComponent<Camera>(_cameraEntity))
            {
                var camera = _world.GetComponent<Camera>(_cameraEntity);
                return camera.GetViewMatrix();
            }
            return Matrix4x4.Identity;
        }
    }

    public Matrix4x4 ProjectionMatrix
    {
        get
        {
            if (_world.HasComponent<Camera>(_cameraEntity))
            {
                var camera = _world.GetComponent<Camera>(_cameraEntity);
                return camera.GetProjectionMatrix();
            }
            return Matrix4x4.Identity;
        }
    }
}

public class GameEngine : IDisposable
{
    private readonly ICameraInputProvider _cameraInput;
    private readonly EngineConfiguration _config;
    private readonly IGameLoop _gameLoop;
    private readonly ILogger<GameEngine> _logger;
    private readonly IRenderingSystem _renderingSystem;
    private readonly IRenderManager _renderManager;
    private readonly WorldManager _worldManager;
    private readonly MaterialManager _materialManager;
    private readonly PostProcessStack _postProcessStack;
    private bool _disposed;

    public GameEngine(
        IGameLoop gameLoop,
        WorldManager worldManager,
        IRenderManager renderManager,
        IRenderingSystem renderingSystem,
        EngineConfiguration config,
        ICameraInputProvider cameraInput,
        MaterialManager materialManager,
        PostProcessStack postProcessStack,
        ILogger<GameEngine> logger)
    {
        _gameLoop = gameLoop ?? throw new ArgumentNullException(nameof(gameLoop));
        _worldManager = worldManager ?? throw new ArgumentNullException(nameof(worldManager));
        _renderManager = renderManager ?? throw new ArgumentNullException(nameof(renderManager));
        _renderingSystem = renderingSystem ?? throw new ArgumentNullException(nameof(renderingSystem));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _cameraInput = cameraInput ?? throw new ArgumentNullException(nameof(cameraInput));
        _materialManager = materialManager ?? throw new ArgumentNullException(nameof(materialManager));
        _postProcessStack = postProcessStack ?? throw new ArgumentNullException(nameof(postProcessStack));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Initialize();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _logger.LogInformation("Disposing game engine");
        _disposed = true;
    }

    public static GameEngine Create(EngineConfiguration? config = null)
    {
        var builder = new EngineBuilder(config);
        return builder.AddCore()
            .AddRendering()
            .AddLogging()
            .Build();
    }

    public void Run()
    {
        if (_disposed)
        {
            _logger.LogError("Cannot run disposed engine");
            return;
        }

        _logger.LogInformation("Starting game engine");
        _gameLoop.Run();
    }

    public void Stop()
    {
        _logger.LogInformation("Stopping game engine");
        _gameLoop.Stop();
    }

    private void Initialize()
    {
        try
        {
            _logger.LogInformation("Initializing game engine...");

            _renderManager.Initialize(
                _config.Window.Width,
                _config.Window.Height,
                _config.Window.Title);

            _postProcessStack.AddEffect(new ToneMappingEffect());

            _renderingSystem.Initialize(_worldManager);

            var aspect = _config.Window.Height <= 0 ? 1.0f : (float)_config.Window.Width / _config.Window.Height;
            var camEntity = _worldManager.CreateEntity();
            var camera = new Camera(fov: MathF.PI / 4f, near: 0.1f, far: 1000f, aspect: aspect)
            {
                Position = new Vector3(0, 30, 120),
                Target = new Vector3(0, 0, 0),
                Up = Vector3.UnitY,
                IsOrthographic = false
            };
            _worldManager.AddComponent(camEntity, camera);

            var cameraAdapter = new CameraAdapter(_worldManager, camEntity);
            _renderManager.SetActiveCamera(cameraAdapter);
            _logger.LogInformation("Camera entity {Entity} registered with RenderManager", camEntity);

            var freeCameraSystem = new FreeCameraSystem(_cameraInput)
            {
                Enabled = true,
                MoveSpeed = 5f,
                LookSpeed = 0.0025f
            };
            _worldManager.AddSystem(freeCameraSystem, 10);

            _worldManager.AddSystem(_renderingSystem);

            new DemoScene(_worldManager, _materialManager, _config, _logger).Populate();

            _logger.LogInformation("Game engine initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize game engine");
            throw;
        }
    }
}