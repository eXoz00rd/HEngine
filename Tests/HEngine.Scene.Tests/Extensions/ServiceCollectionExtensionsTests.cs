using System.Numerics;
using HEngine.Core.Components.Rendering;
using HEngine.Core.Components.Transform;
using HEngine.Core.Contracts;
using HEngine.Core.Managers;
using HEngine.Core.Systems;
using HEngine.ECS.Extensions;
using HEngine.Scene.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace HEngine.Scene.Tests.Extensions;

public class ServiceCollectionExtensionsTests
{
    private static ServiceProvider BuildProvider(bool withCameraInput = true)
    {
        var services = new ServiceCollection();
        services.AddHEngineECS();
        services.AddHEngineScene();

        if (withCameraInput)
            services.AddSingleton<ICameraInputProvider>(new FakeInput(new Vector3(0, 0, 1)));

        return services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    [Fact(DisplayName = "AddHEngineScene adds the three scene systems to the resolved world")]
    public void AddHEngineScene_AddsSceneSystemsToWorld()
    {
        using var provider = BuildProvider();

        var world = provider.GetRequiredService<WorldManager>();

        Assert.True(world.HasSystem<FreeCameraSystem>());
        Assert.True(world.HasSystem<TransformHierarchySystem>());
        Assert.True(world.HasSystem<FrustumCullingSystem>());
    }

    [Fact(DisplayName = "Scene systems tick in input, hierarchy, culling order")]
    public void SceneSystems_TickInDeclaredOrder()
    {
        using var provider = BuildProvider();
        provider.GetRequiredService<WorldManager>();
        var systemManager = provider.GetRequiredService<SystemManager>();

        systemManager.Update(0.016f);

        Assert.Equal(
            [nameof(FreeCameraSystem), nameof(TransformHierarchySystem), nameof(FrustumCullingSystem)],
            systemManager.GetSystemNames());
    }

    [Fact(DisplayName = "Scene systems act on the world when the shared SystemManager is updated")]
    public void SceneSystems_ActOnWorldWhenSystemManagerUpdates()
    {
        using var provider = BuildProvider();
        var world = provider.GetRequiredService<WorldManager>();

        var cameraEntity = world.CreateEntity();
        world.AddComponent(cameraEntity, new Camera
        {
            Position = new Vector3(0, 0, 5),
            Target = Vector3.Zero,
            Up = Vector3.UnitY,
            FieldOfView = MathF.PI / 2f,
            NearPlane = 0.1f,
            FarPlane = 100f,
            AspectRatio = 16f / 9f,
            IsOrthographic = false
        });

        var farEntity = world.CreateEntity();
        world.AddComponent(farEntity, new Transform(new Vector3(1000, 0, 0)));
        world.AddComponent(farEntity, new BoundingBox(Vector3.Zero, new Vector3(0.5f)));

        provider.GetRequiredService<SystemManager>().Update(1f);

        Assert.True(world.HasComponent<Culled>(farEntity));
        Assert.Equal(new Vector3(0, 0, 0), world.GetComponent<Camera>(cameraEntity).Position);
    }

    [Fact(DisplayName = "AddHEngineScene without a camera input provider fails provider validation at startup")]
    public void AddHEngineScene_WithoutCameraInput_FailsValidation()
    {
        Assert.Throws<AggregateException>(() => BuildProvider(withCameraInput: false));
    }

    private sealed class FakeInput : ICameraInputProvider
    {
        private readonly Vector3 _movement;

        public FakeInput(Vector3 movement)
        {
            _movement = movement;
        }

        public Vector3 GetMovementAxes() => _movement;

        public Vector2 GetLookDelta() => Vector2.Zero;
    }
}
