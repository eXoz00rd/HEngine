using System.Numerics;
using HEngine.Builders;
using CoreDirectionalLight = HEngine.Core.Components.Rendering.DirectionalLight;
using CorePointLight = HEngine.Core.Components.Rendering.PointLight;
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
        _logger.LogInformation("Creating presentation scene...");

        // Ground plane
        var groundEntity = _worldManager.CreateEntity();
        _worldManager.AddComponent(groundEntity, new Transform
        {
            Position = new Vector3(0, -0.01f, 0),
            Rotation = Quaternion.Identity,
            Scale = new Vector3(100, 1, 100)
        });
        _worldManager.AddComponent(groundEntity, new Mesh
        {
            VertexArrayId = 2,
            IndexCount = 6,
            Color = new Vector4(0.2f, 0.4f, 0.2f, 1.0f)
        });

        // Central pyramid of cubes (5x5 base)
        const int pyramidRows = 4;
        const float cubeSize = 1.5f;
        const float cubeGap = 0.15f;
        const float cubeSpacing = cubeSize + cubeGap;

        for (int row = 0; row < pyramidRows; row++)
        {
            int cols = pyramidRows - row;
            float rowOffset = (cols - 1) * cubeSpacing * 0.5f;
            float yPos = row * cubeSpacing;

            for (int col = 0; col < cols; col++)
            {
                var cubeEntity = _worldManager.CreateEntity();
                float posX = -rowOffset + col * cubeSpacing;
                Vector3 position = new(posX, yPos, 0);
                float rotationAngle = (float)(row * 0.3 + col * 0.15);

                var t = (float)row / (pyramidRows - 1);
                var color = new Vector4(
                    0.5f + 0.5f * MathF.Cos(t * MathF.PI),
                    0.5f + 0.5f * MathF.Sin(t * MathF.PI),
                    0.3f + 0.3f * t,
                    1.0f
                );

                _worldManager.AddComponent(cubeEntity, new Transform
                {
                    Position = position,
                    Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, rotationAngle),
                    Scale = Vector3.One
                });
                _worldManager.AddComponent(cubeEntity, new Mesh
                {
                    VertexArrayId = 1,
                    IndexCount = 36,
                    Color = color
                });
            }
        }

        // Sphere at center top
        var sphereEntity = _worldManager.CreateEntity();
        _worldManager.AddComponent(sphereEntity, new Transform
        {
            Position = new Vector3(0, pyramidRows * cubeSpacing + 1.5f, 0),
            Rotation = Quaternion.Identity,
            Scale = new Vector3(1.2f, 1.2f, 1.2f)
        });
        _worldManager.AddComponent(sphereEntity, new Mesh
        {
            VertexArrayId = 3,
            IndexCount = 36,
            Color = new Vector4(0.8f, 0.2f, 0.9f, 1.0f)
        });

        // Floating ring of cubes around the pyramid
        const int ringCount = 16;
        const float ringRadius = 8f;
        const float ringHeight = pyramidRows * cubeSpacing + 3f;

        for (int i = 0; i < ringCount; i++)
        {
            var ringEntity = _worldManager.CreateEntity();
            float angle = (i / (float)ringCount) * MathF.Tau;
            Vector3 position = new(
                MathF.Cos(angle) * ringRadius,
                ringHeight + MathF.Sin(angle * 2) * 0.5f,
                MathF.Sin(angle) * ringRadius
            );
            float hue = i / (float)ringCount;
            Vector3 cosHue = new(
                0.5f + 0.5f * MathF.Cos(hue * MathF.Tau),
                0.5f + 0.5f * MathF.Cos(hue * MathF.Tau + 2.094f),
                0.5f + 0.5f * MathF.Cos(hue * MathF.Tau + 4.189f)
            );

            _worldManager.AddComponent(ringEntity, new Transform
            {
                Position = position,
                Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, angle + MathF.PI * 0.25f),
                Scale = new Vector3(0.6f, 0.6f, 0.6f)
            });
            _worldManager.AddComponent(ringEntity, new Mesh
            {
                VertexArrayId = 1,
                IndexCount = 36,
                Color = new Vector4(cosHue.X, cosHue.Y, cosHue.Z, 1.0f)
            });
        }

        // Row of smaller cubes in front (color gradient arc)
        const int arcCount = 12;
        const float arcRadius = 12f;

        for (int i = 0; i < arcCount; i++)
        {
            var arcEntity = _worldManager.CreateEntity();
            float angle = MathF.PI * 0.1f + (i / (float)(arcCount - 1)) * MathF.PI * 0.8f;
            Vector3 position = new(
                MathF.Cos(angle) * arcRadius,
                0.75f,
                MathF.Sin(angle) * arcRadius
            );

            float hue = i / (float)(arcCount - 1);
            Vector3 cosHue = new(
                0.5f + 0.5f * MathF.Cos(hue * MathF.Tau),
                0.5f + 0.5f * MathF.Cos(hue * MathF.Tau + 2.094f),
                0.5f + 0.5f * MathF.Cos(hue * MathF.Tau + 4.189f)
            );

            _worldManager.AddComponent(arcEntity, new Transform
            {
                Position = position,
                Rotation = Quaternion.Identity,
                Scale = Vector3.One
            });
            _worldManager.AddComponent(arcEntity, new Mesh
            {
                VertexArrayId = 1,
                IndexCount = 36,
                Color = new Vector4(cosHue.X, cosHue.Y, cosHue.Z, 1.0f)
            });
        }

        // 2D sprite badges floating above scene (pixel coordinates, Y=0 is screen top)
        var badgeSize = new Vector2(120f, 36f);
        var badgeY = 16f;
        var halfWidth = (_config.Window.Width / 2f) - (badgeSize.X / 2f);

        // Left badge - centered horizontally in the left half
        var badgeLeftEntity = _worldManager.CreateEntity();
        _worldManager.AddComponent(badgeLeftEntity, new Transform2D
        {
            Position = new Vector2(halfWidth - badgeSize.X - 10, badgeY)
        });
        _worldManager.AddComponent(badgeLeftEntity, new Sprite
        {
            Size = badgeSize,
            Color = new Vector4(0.1f, 0.7f, 0.9f, 0.9f),
            Origin = new Vector2(0.5f, 0.5f)
        });

        // Right badge - centered horizontally in the right half
        var badgeRightEntity = _worldManager.CreateEntity();
        _worldManager.AddComponent(badgeRightEntity, new Transform2D
        {
            Position = new Vector2(halfWidth + 10, badgeY)
        });
        _worldManager.AddComponent(badgeRightEntity, new Sprite
        {
            Size = badgeSize,
            Color = new Vector4(0.9f, 0.3f, 0.7f, 0.9f),
            Origin = new Vector2(0.5f, 0.5f)
        });

        // Corner decorative sprites (actual corners of the screen)
        var cornerSize = new Vector2(40f, 40f);
        var corners = new[]
        {
            (new Vector2(cornerSize.X / 2f, cornerSize.Y / 2f), new Vector4(1.0f, 0.8f, 0.1f, 0.85f)),
            (new Vector2(_config.Window.Width - cornerSize.X / 2f, cornerSize.Y / 2f), new Vector4(0.1f, 1.0f, 0.3f, 0.85f)),
            (new Vector2(cornerSize.X / 2f, _config.Window.Height - cornerSize.Y / 2f), new Vector4(0.8f, 0.2f, 1.0f, 0.85f)),
            (new Vector2(_config.Window.Width - cornerSize.X / 2f, _config.Window.Height - cornerSize.Y / 2f), new Vector4(1.0f, 0.3f, 0.3f, 0.85f)),
        };
        foreach (var (pos, col) in corners)
        {
            var cornerEntity = _worldManager.CreateEntity();
            _worldManager.AddComponent(cornerEntity, new Transform2D
            {
                Position = pos
            });
            _worldManager.AddComponent(cornerEntity, new Sprite
            {
                Size = cornerSize,
                Color = col,
                Origin = new Vector2(0.5f, 0.5f)
            });
        }

        // Directional light (sun)
        var dirLightEntity = _worldManager.CreateEntity();
        _worldManager.AddComponent(dirLightEntity, new Transform
        {
            Position = Vector3.Zero,
            Rotation = Quaternion.Identity,
            Scale = Vector3.One
        });
        _worldManager.AddComponent(dirLightEntity, new CoreDirectionalLight(
            new Vector3(0.5f, -1.0f, 0.3f),
            new Vector3(1.0f, 0.95f, 0.8f)
        ));

        // Point lights (colored glowing orbs with actual PointLight components)
        var pointLightConfigs = new[]
        {
            (new Vector3(-6, 3, -4), new Vector3(1.0f, 0.2f, 0.2f), new Vector4(1.0f, 0.3f, 0.3f, 1.0f)),
            (new Vector3(6, 3, -4), new Vector3(0.2f, 0.2f, 1.0f), new Vector4(0.3f, 0.3f, 1.0f, 1.0f)),
            (new Vector3(0, 3, 6), new Vector3(0.2f, 1.0f, 0.2f), new Vector4(0.3f, 1.0f, 0.3f, 1.0f)),
        };

        foreach (var (pos, lightColor, orbColor) in pointLightConfigs)
        {
            var lightEntity = _worldManager.CreateEntity();
            _worldManager.AddComponent(lightEntity, new Transform
            {
                Position = pos,
                Rotation = Quaternion.Identity,
                Scale = new Vector3(0.3f, 0.3f, 0.3f)
            });
            _worldManager.AddComponent(lightEntity, new Mesh
            {
                VertexArrayId = 3,
                IndexCount = 36,
                Color = orbColor
            });
            _worldManager.AddComponent(lightEntity, new CorePointLight(lightColor, intensity: 3.0f, range: 15f));
        }

        // Background sky plane (flat on ground at high Y, no rotation needed)
        var skyEntity = _worldManager.CreateEntity();
        _worldManager.AddComponent(skyEntity, new Transform
        {
            Position = new Vector3(0, 45, 0),
            Rotation = Quaternion.Identity,
            Scale = new Vector3(200, 1, 200)
        });
        _worldManager.AddComponent(skyEntity, new Mesh
        {
            VertexArrayId = 2,
            IndexCount = 6,
            Color = new Vector4(0.15f, 0.2f, 0.4f, 1.0f)
        });

        // Background back wall (standing vertical, rotated X by 90 degrees from flat plane)
        // Note: VA0=2 is a flat Y-up plane. To make it stand vertically facing -Z,
        // we rotate it -90 degrees around X axis so the normal points toward -Z
        var backWallEntity = _worldManager.CreateEntity();
        _worldManager.AddComponent(backWallEntity, new Transform
        {
            Position = new Vector3(0, 22.5f, -50),
            Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, -MathF.PI / 2f),
            Scale = new Vector3(100, 1, 1)
        });
        _worldManager.AddComponent(backWallEntity, new Mesh
        {
            VertexArrayId = 2,
            IndexCount = 6,
            Color = new Vector4(0.1f, 0.12f, 0.2f, 1.0f)
        });

        // Side wall planes for depth (standing vertical)
        var leftWallEntity = _worldManager.CreateEntity();
        _worldManager.AddComponent(leftWallEntity, new Transform
        {
            Position = new Vector3(-30, 10, 0),
            Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, -MathF.PI / 2f),
            Scale = new Vector3(1, 1, 60)
        });
        _worldManager.AddComponent(leftWallEntity, new Mesh
        {
            VertexArrayId = 2,
            IndexCount = 6,
            Color = new Vector4(0.25f, 0.25f, 0.35f, 1.0f)
        });

        var rightWallEntity = _worldManager.CreateEntity();
        _worldManager.AddComponent(rightWallEntity, new Transform
        {
            Position = new Vector3(30, 10, 0),
            Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, -MathF.PI / 2f),
            Scale = new Vector3(1, 1, 60)
        });
        _worldManager.AddComponent(rightWallEntity, new Mesh
        {
            VertexArrayId = 2,
            IndexCount = 6,
            Color = new Vector4(0.25f, 0.25f, 0.35f, 1.0f)
        });

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Presentation scene created: ground, pyramid, sphere, ring, arc, badges, lights, sky, walls");
        }
    }
}