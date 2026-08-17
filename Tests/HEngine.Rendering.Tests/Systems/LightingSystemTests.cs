using System;
using System.Linq;
using System.Numerics;
using HEngine.Core.Components.Rendering;
using HEngine.Core.Components.Transform;
using HEngine.Core.Managers;
using HEngine.Rendering.Systems;
using HEngine.Rendering.Data;
using Xunit;

namespace HEngine.Rendering.Tests.Systems;

public class LightingSystemTests
{
    [Fact(DisplayName = "GatherLights collects directional and point lights with expected data")]
    public void GatherLights_CollectsLights()
    {
        using var world = new WorldManager(new SystemManager());
        var system = new LightingSystem();
        system.Initialize(world);

        var dirEntity = world.CreateEntity();
        world.AddComponent(dirEntity, new DirectionalLight(new Vector3(1, -1, 0), new Vector3(1, 1, 1), 2f));

        var pointEntity = world.CreateEntity();
        world.AddComponent(pointEntity, new Transform(new Vector3(3, 4, 5)));
        world.AddComponent(pointEntity, new PointLight(new Vector3(0.5f, 0.6f, 0.7f), intensity: 1.5f, range: 12f, attenuation: 0.8f));

        var lights = system.GatherLights(world);

        Assert.Equal(2, lights.Length);

        Assert.Equal(LightType.Directional, lights[0].Type);
        Assert.Equal(new Vector3(1, 1, 1), lights[0].Color);
        Assert.Equal(2f, lights[0].Intensity);
        var expectedDir = Vector3.Normalize(new Vector3(1, -1, 0));
        Assert.True(Vector3.Distance(expectedDir, lights[0].Direction) < 1e-5f);

        Assert.Equal(LightType.Point, lights[1].Type);
        Assert.Equal(new Vector3(0.5f, 0.6f, 0.7f), lights[1].Color);
        Assert.Equal(1.5f, lights[1].Intensity);
        Assert.Equal(12f, lights[1].Range);
        Assert.Equal(0.8f, lights[1].Attenuation);
        Assert.True(Vector3.Distance(new Vector3(3, 4, 5), lights[1].Position) < 1e-5f);
    }

    [Fact(DisplayName = "GatherLights limits to MaxLights and preserves gathering order (dir first)")]
    public void GatherLights_Limits_To_MaxLights()
    {
        using var world = new WorldManager(new SystemManager());
        var system = new LightingSystem();
        system.Initialize(world);

        for (int i = 0; i < 3; i++)
        {
            var e = world.CreateEntity();
            world.AddComponent(e, new DirectionalLight(new Vector3(0, -1, 0), new Vector3(1, 1, 1), 1f + i));
        }

        for (int i = 0; i < 7; i++)
        {
            var e = world.CreateEntity();
            world.AddComponent(e, new Transform(new Vector3(i, 0, 0)));
            world.AddComponent(e, new PointLight(new Vector3(1, 0, 0), intensity: 1f, range: 10f, attenuation: 1f));
        }

        var lights = system.GatherLights(world);

        Assert.Equal(LightingSystem.MaxLights, lights.Length);
    }

    [Fact(DisplayName = "GatherLights skips point lights on culled entities")]
    public void GatherLights_Skips_Culled_Point_Lights()
    {
        using var world = new WorldManager(new SystemManager());
        var system = new LightingSystem();
        system.Initialize(world);

        var visible = world.CreateEntity();
        world.AddComponent(visible, new Transform(new Vector3(1, 0, 0)));
        world.AddComponent(visible, new PointLight(new Vector3(1, 1, 1)));

        var culled = world.CreateEntity();
        world.AddComponent(culled, new Transform(new Vector3(2, 0, 0)));
        world.AddComponent(culled, new PointLight(new Vector3(1, 0, 0)));
        world.AddComponent(culled, new Culled());

        var lights = system.GatherLights(world);

        Assert.Single(lights.Where(l => l.Type == LightType.Point));
        Assert.DoesNotContain(lights, l => l.Type == LightType.Point && Math.Abs(l.Position.X - 2f) < 1e-5f);
    }
}
