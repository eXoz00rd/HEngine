using System.Numerics;
using HEngine.Core.Components.Rendering;
using HEngine.Core.Components.Transform;
using HEngine.Core.Managers;
using HEngine.Rendering.Data;
using HEngine.Rendering.Managers;
using HEngine.Rendering.Systems;

namespace HEngine.Rendering.Tests.Systems;

public class PBRLightingSystemTests
{
    [Fact(DisplayName = "MaxLights is 8 for PBR pipeline")]
    public void MaxLights_Is_Eight()
    {
        Assert.Equal(8, LightingSystem.MaxLights);
    }

    [Fact(DisplayName = "GatherLights collects SpotLight with direction and cone angles")]
    public void GatherLights_CollectsSpotLight()
    {
        using var world = new WorldManager(new SystemManager());
        var system = new LightingSystem();
        system.Initialize(world);

        var entity = world.CreateEntity();
        world.AddComponent(entity, new Transform(new Vector3(1, 5, 1)));
        world.AddComponent(entity, new SpotLight(
            new Vector3(0, -1, 0),
            new Vector3(1, 0.9f, 0.8f),
            intensity: 2f,
            range: 15f,
            innerAngle: 20f,
            outerAngle: 35f));

        var lights = system.GatherLights(world);

        Assert.Single(lights);
        Assert.Equal(LightType.Spot, lights[0].Type);
        Assert.Equal(new Vector3(1, 0.9f, 0.8f), lights[0].Color);
        Assert.Equal(2f, lights[0].Intensity);
        Assert.Equal(15f, lights[0].Range);
        Assert.True(lights[0].InnerConeAngle > 0);
        Assert.True(lights[0].OuterConeAngle > lights[0].InnerConeAngle);
        Assert.True(Vector3.Distance(lights[0].Position, new Vector3(1, 5, 1)) < 1e-4f);
    }

    [Fact(DisplayName = "GatherLights skips spot lights on culled entities")]
    public void GatherLights_SkipsCulledSpotLights()
    {
        using var world = new WorldManager(new SystemManager());
        var system = new LightingSystem();
        system.Initialize(world);

        var visible = world.CreateEntity();
        world.AddComponent(visible, new Transform(new Vector3(0, 5, 0)));
        world.AddComponent(visible, new SpotLight(new Vector3(0, -1, 0), new Vector3(1, 1, 1)));

        var culled = world.CreateEntity();
        world.AddComponent(culled, new Transform(new Vector3(10, 5, 10)));
        world.AddComponent(culled, new SpotLight(new Vector3(0, -1, 0), new Vector3(1, 0, 0)));
        world.AddComponent(culled, new Culled());

        var lights = system.GatherLights(world);

        Assert.Single(lights);
        Assert.Equal(LightType.Spot, lights[0].Type);
    }

    [Fact(DisplayName = "GatherLights collects directional, point and spot lights together up to MaxLights")]
    public void GatherLights_CollectsMixedLights_UpToMax()
    {
        using var world = new WorldManager(new SystemManager());
        var system = new LightingSystem();
        system.Initialize(world);

        for (int i = 0; i < 3; i++)
        {
            var e = world.CreateEntity();
            world.AddComponent(e, new DirectionalLight(Vector3.UnitY, Vector3.One));
        }

        for (int i = 0; i < 3; i++)
        {
            var e = world.CreateEntity();
            world.AddComponent(e, new Transform(new Vector3(i, 0, 0)));
            world.AddComponent(e, new PointLight(Vector3.One, range: 10f));
        }

        for (int i = 0; i < 4; i++)
        {
            var e = world.CreateEntity();
            world.AddComponent(e, new Transform(new Vector3(i, 5, 0)));
            world.AddComponent(e, new SpotLight(Vector3.UnitY, Vector3.One));
        }

        var lights = system.GatherLights(world);

        Assert.Equal(LightingSystem.MaxLights, lights.Length);
    }
}

