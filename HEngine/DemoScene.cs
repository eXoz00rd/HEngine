using System.Numerics;
using HEngine.Core.Components.Rendering;
using HEngine.Core.Components.Transform;
using HEngine.Core.Configuration;
using HEngine.Core.Managers;
using HEngine.Rendering.Components;
using HEngine.Rendering.Data;
using HEngine.Rendering.Managers;
using Microsoft.Extensions.Logging;

namespace HEngine;

public sealed class DemoScene
{
    private readonly WorldManager _worldManager;
    private readonly MaterialManager _materialManager;
    private readonly EngineConfiguration _config;
    private readonly ILogger _logger;

    public DemoScene(
        WorldManager worldManager,
        MaterialManager materialManager,
        EngineConfiguration config,
        ILogger logger)
    {
        _worldManager = worldManager ?? throw new ArgumentNullException(nameof(worldManager));
        _materialManager = materialManager ?? throw new ArgumentNullException(nameof(materialManager));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Populate()
    {
        _logger.LogInformation("Creating presentation scene...");

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
                _worldManager.AddComponent(cubeEntity, new Renderable());
            }
        }

        var checkerTexturePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Textures", "demo_checker.png");
        var checkerMaterial = new Material { DiffuseTexture = checkerTexturePath };
        var checkerMaterialId = _materialManager.RegisterWithId("DemoChecker", checkerMaterial);

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
        _worldManager.AddComponent(sphereEntity, new Renderable { MaterialId = checkerMaterialId });

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
            _worldManager.AddComponent(ringEntity, new Renderable());
        }

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
            _worldManager.AddComponent(arcEntity, new Renderable());
        }

        var badgeSize = new Vector2(120f, 36f);
        var badgeY = 16f;
        var halfWidth = (_config.Window.Width / 2f) - (badgeSize.X / 2f);

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

        var dirLightEntity = _worldManager.CreateEntity();
        _worldManager.AddComponent(dirLightEntity, new Transform
        {
            Position = Vector3.Zero,
            Rotation = Quaternion.Identity,
            Scale = Vector3.One
        });
        _worldManager.AddComponent(dirLightEntity, new DirectionalLight(
            new Vector3(0.5f, -1.0f, 0.3f),
            new Vector3(1.0f, 0.95f, 0.8f)
        ));

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
            _worldManager.AddComponent(lightEntity, new PointLight(lightColor, intensity: 3.0f, range: 15f));
        }

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
