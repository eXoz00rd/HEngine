using System.Numerics;
using HEngine.Core.Components.Rendering;
using HEngine.Core.Components.Transform;
using HEngine.Core.Managers;
using HEngine.Core.Rendering.Contracts;
using HEngine.Rendering.Components;
using HEngine.Rendering.Systems;

namespace HEngine.Rendering.Tests.Systems;

file sealed class FakeShadowRenderer : IShadowRenderer
{
    public int BeginCallCount { get; private set; }
    public int EndCallCount { get; private set; }
    public int RenderMeshCallCount { get; private set; }
    public List<(int Cascade, Matrix4x4 LightVP)> ShadowPassBegins { get; } = new();

    public void BeginShadowPass(int cascadeIndex, Matrix4x4 lightVP, int resolution)
    {
        BeginCallCount++;
        ShadowPassBegins.Add((cascadeIndex, lightVP));
    }

    public void RenderDepthOnlyMesh(Matrix4x4 transform, ReadOnlySpan<float> vertices, ReadOnlySpan<uint> indices)
    {
        RenderMeshCallCount++;
    }

    public void EndShadowPass()
    {
        EndCallCount++;
    }

    public void BindShadowResources(ReadOnlySpan<Matrix4x4> lightVPs, ReadOnlySpan<float> cascadeSplits) { }
}

public class ShadowPassTests
{
    private static Camera CreateCamera() => new(MathF.PI / 4f, 0.1f, 100f, 16f / 9f)
    {
        Position = new Vector3(0, 10, 20),
        Target = Vector3.Zero,
        Up = Vector3.UnitY
    };

    [Fact(DisplayName = "ShadowRenderingSystem skips entities with CastShadows=false")]
    public void ShadowPass_Skips_Entities_With_CastShadows_False()
    {
        using var world = new WorldManager();
        var system = new ShadowRenderingSystem();
        system.Initialize(world);

        var caster = world.CreateEntity();
        world.AddComponent(caster, new Transform(new Vector3(0, 0, 0)));
        world.AddComponent(caster, new HEngine.Rendering.Components.Mesh(1, 36));
        world.AddComponent(caster, new HEngine.Core.Components.Rendering.Renderable
        {
            CastShadows = true,
            ReceiveShadows = true,
            IsVisible = true
        });

        var nonCaster = world.CreateEntity();
        world.AddComponent(nonCaster, new Transform(new Vector3(5, 0, 0)));
        world.AddComponent(nonCaster, new HEngine.Rendering.Components.Mesh(1, 36));
        world.AddComponent(nonCaster, new HEngine.Core.Components.Rendering.Renderable
        {
            CastShadows = false,
            ReceiveShadows = false,
            IsVisible = true
        });

        var fakeRenderer = new FakeShadowRenderer();
        system.SetShadowRenderer(fakeRenderer);

        var camera = CreateCamera();
        system.RenderShadows(camera, new Vector3(-1f, -2f, -1f), [100f], 2048);

        Assert.Equal(1, fakeRenderer.RenderMeshCallCount);
    }

    [Fact(DisplayName = "ShadowRenderingSystem skips culled entities")]
    public void ShadowPass_Skips_Culled_Entities()
    {
        using var world = new WorldManager();
        var system = new ShadowRenderingSystem();
        system.Initialize(world);

        var visible = world.CreateEntity();
        world.AddComponent(visible, new Transform(new Vector3(0, 0, 0)));
        world.AddComponent(visible, new HEngine.Rendering.Components.Mesh(1, 36));
        world.AddComponent(visible, new HEngine.Core.Components.Rendering.Renderable { CastShadows = true });

        var culled = world.CreateEntity();
        world.AddComponent(culled, new Transform(new Vector3(5, 0, 0)));
        world.AddComponent(culled, new HEngine.Rendering.Components.Mesh(1, 36));
        world.AddComponent(culled, new HEngine.Core.Components.Rendering.Renderable { CastShadows = true });
        world.AddComponent(culled, new HEngine.Core.Components.Rendering.Culled());

        var fakeRenderer = new FakeShadowRenderer();
        system.SetShadowRenderer(fakeRenderer);

        var camera = CreateCamera();
        system.RenderShadows(camera, new Vector3(-1f, -2f, -1f), [100f], 2048);

        Assert.Equal(1, fakeRenderer.RenderMeshCallCount);
    }

    [Fact(DisplayName = "ShadowRenderingSystem creates one shadow pass per cascade")]
    public void ShadowPass_Creates_One_Pass_Per_Cascade()
    {
        using var world = new WorldManager();
        var system = new ShadowRenderingSystem();
        system.Initialize(world);

        var fakeRenderer = new FakeShadowRenderer();
        system.SetShadowRenderer(fakeRenderer);

        var camera = CreateCamera();
        system.RenderShadows(camera, Vector3.Normalize(new Vector3(-1f, -1f, 0f)), [10f, 50f, 100f, 200f], 2048);

        Assert.Equal(4, fakeRenderer.BeginCallCount);
        Assert.Equal(4, fakeRenderer.EndCallCount);
    }

    [Fact(DisplayName = "ShadowRenderingSystem does nothing when no shadow renderer")]
    public void ShadowPass_DoesNothing_When_No_ShadowRenderer()
    {
        using var world = new WorldManager();
        var system = new ShadowRenderingSystem();
        system.Initialize(world);

        var camera = CreateCamera();
        var exception = Record.Exception(() =>
            system.RenderShadows(camera, Vector3.UnitY, [100f], 2048));

        Assert.Null(exception);
    }

    [Fact(DisplayName = "ShadowRenderingSystem cascade indices passed in order")]
    public void ShadowPass_CascadeIndices_In_Order()
    {
        using var world = new WorldManager();
        var system = new ShadowRenderingSystem();
        system.Initialize(world);

        var fakeRenderer = new FakeShadowRenderer();
        system.SetShadowRenderer(fakeRenderer);

        var camera = CreateCamera();
        system.RenderShadows(camera, new Vector3(-1f, -1f, 0f), [10f, 50f, 200f], 2048);

        Assert.Equal(3, fakeRenderer.ShadowPassBegins.Count);
        for (int i = 0; i < 3; i++)
            Assert.Equal(i, fakeRenderer.ShadowPassBegins[i].Cascade);
    }
}


