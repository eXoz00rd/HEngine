using System.Numerics;
using HEngine.Builders;
using HEngine.Core.Components.Rendering;
using HEngine.Core.Components.Transform;
using HEngine.Core.Configuration;
using HEngine.Core.Contracts;
using HEngine.Core.Managers;
using HEngine.Core.Primitives;
using HEngine.Core.Rendering.Contracts;
using HEngine.Core.Systems;
using HEngine.Rendering.Components;
using HEngine.Rendering.Systems.Implementations;
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
    private readonly SystemManager _systemManager;
    private readonly WorldManager _worldManager;
    private bool _disposed;

    public GameEngine(
        IGameLoop gameLoop,
        SystemManager systemManager,
        WorldManager worldManager,
        IRenderManager renderManager,
        IRenderingSystem renderingSystem,
        EngineConfiguration config,
        ICameraInputProvider cameraInput,
        ILogger<GameEngine> logger)
    {
        _gameLoop = gameLoop ?? throw new ArgumentNullException(nameof(gameLoop));
        _systemManager = systemManager ?? throw new ArgumentNullException(nameof(systemManager));
        _worldManager = worldManager ?? throw new ArgumentNullException(nameof(worldManager));
        _renderManager = renderManager ?? throw new ArgumentNullException(nameof(renderManager));
        _renderingSystem = renderingSystem ?? throw new ArgumentNullException(nameof(renderingSystem));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _cameraInput = cameraInput ?? throw new ArgumentNullException(nameof(cameraInput));
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

            _renderingSystem.Initialize(_worldManager);

            if (_renderManager.TryGetRenderContext(out var renderContext) && _renderingSystem is RenderingSystem rs)
            {
                rs.SetRenderContext(renderContext);
            }

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
            freeCameraSystem.Initialize(_worldManager);
            _systemManager.AddSystem(freeCameraSystem, 10);

            _systemManager.AddSystem(_renderingSystem);

            CreateExampleEntities();


            _logger.LogInformation("Game engine initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize game engine");
            throw;
        }
    }

    private void CreateExampleEntities()
    {
        _logger.LogInformation("Creating example entities...");

        var size = new Vector2(64, 64);
        var centerPos = new Vector2(
            _config.Window.Width * 0.5f - size.X * 0.5f,
            _config.Window.Height * 0.5f - size.Y * 0.5f
        );

        var eCenter = _worldManager.CreateEntity();
        _worldManager.AddComponent(
            eCenter,
            new Transform2D { Position = centerPos }
        );
        _worldManager.AddComponent(
            eCenter,
            new Sprite
            {
                Size = size,
                Color = new Vector4(0.25f, 0.5f, 1f, 1f),
                Origin = new Vector2(0.5f, 0.5f)
            }
        );

        var gap = 12f;
        var rightPos = new Vector2(centerPos.X + size.X + gap, centerPos.Y);
        var eRight = _worldManager.CreateEntity();
        _worldManager.AddComponent(
            eRight,
            new Transform2D { Position = rightPos }
        );
        _worldManager.AddComponent(
            eRight,
            new Sprite
            {
                Size = size,
                Color = new Vector4(1f, 0.5f, 0.1f, 1f),
                Origin = new Vector2(0.5f, 0.5f)
            }
        );

        const int cubeCount = 100;
        const float cubeSize = 1.0f;
        const float cubeGap = 0.5f;
        const float spacing = cubeSize + cubeGap;
        const float totalWidth = cubeCount * spacing;
        const float startX = -(totalWidth / 2f);

        for (int i = 0; i < cubeCount; i++)
        {
            var cubeEntity = _worldManager.CreateEntity();

            var posX = startX + (i * spacing) + (cubeSize / 2f);
            var position = new Vector3(posX, 0, 0);

            var t = i / (float)(cubeCount - 1);
            var color = new Vector4(
                t,
                1.0f - t,
                0.5f,
                1.0f
            );

            _worldManager.AddComponent(cubeEntity, new Transform
            {
                Position = position,
                Rotation = Quaternion.Identity,
                Scale = Vector3.One
            });

            _worldManager.AddComponent(cubeEntity, new Mesh
            {
                VertexArrayId = 1,
                IndexCount = 36,
                MaterialPath = "",
                Color = color
            });
        }

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Created sprite entities: center={Center}, right={Right}", eCenter, eRight);
            _logger.LogInformation("Successfully created {CubeCount} cube entities", cubeCount);
        }
    }
}