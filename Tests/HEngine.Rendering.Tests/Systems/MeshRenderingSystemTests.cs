using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using HEngine.Core.Components.Rendering;
using HEngine.Core.Components.Transform;
using HEngine.Core.Managers;
using HEngine.Core.Rendering.Contracts;
using HEngine.Core.Rendering.Data;
using HEngine.Rendering.Components;
using HEngine.Rendering.Systems;
using Xunit;

namespace HEngine.Rendering.Tests.Systems;

file sealed class FakeRenderer : IRenderer
{
    public bool IsInitialized { get; private set; }
    public bool ShouldClose { get; private set; }

    public readonly List<(Matrix4x4 Transform, int VertexCount, int IndexCount)> MeshDraws = new();

    public void Initialize(int width, int height, string title)
    {
        IsInitialized = true;
    }

    public void BeginFrame() { }
    public void EndFrame() { }
    public void Clear(Vector4 clearColor) { }
    public void Present() { }
    public void PollEvents() { }

    public void SetViewMatrix(Matrix4x4 viewMatrix) { }
    public void SetProjectionMatrix(Matrix4x4 projectionMatrix) { }
    public void SetLights(ReadOnlySpan<LightData> lights) { }

    public void DrawSprite(Vector2 position, Vector2 size, Vector4 color) { }

    public void FlushBatch() { }

    public void DrawMesh(Matrix4x4 transform, ReadOnlySpan<float> vertices, ReadOnlySpan<uint> indices)
    {
        MeshDraws.Add((transform, vertices.Length, indices.Length));
    }

    public void Run() { }

    public void Dispose() { }
}

file sealed class FakeRenderContext : IRenderContext
{
    public IRenderer Renderer { get; }
    public Matrix4x4 ViewMatrix { get; set; } = Matrix4x4.Identity;
    public Matrix4x4 ProjectionMatrix { get; set; } = Matrix4x4.Identity;
    public Vector4 ClearColor { get; set; } = new(0, 0, 0, 1);

    public FakeRenderContext(IRenderer renderer)
    {
        Renderer = renderer;
    }
}

public class MeshRenderingSystemTests
{
    [Fact(DisplayName = "Render throws when called before Initialize")]
    public void Render_Throws_When_Not_Initialized()
    {
        var system = new MeshRenderingSystem();
        var fakeRenderer = new FakeRenderer();
        var context = new FakeRenderContext(fakeRenderer);

        Assert.Throws<InvalidOperationException>(() => system.Render(context));
    }

    [Fact(DisplayName = "Render with two mesh entities should call DrawMesh twice")]
    public void Render_WithMeshEntities_CallsDrawMesh()
    {
        var world = new WorldManager(new SystemManager());
        var system = new MeshRenderingSystem();
        system.Initialize(world);

        var a = world.CreateEntity();
        world.AddComponent(a, new Transform(new Vector3(1, 2, 3)));
        world.AddComponent(a, new Mesh(1, 36));

        var b = world.CreateEntity();
        world.AddComponent(b, new Transform(new Vector3(-5, 0, 0)));
        world.AddComponent(b, new Mesh(2, 12));

        var fakeRenderer = new FakeRenderer();
        var context = new FakeRenderContext(fakeRenderer);

        system.Render(context);

        Assert.Equal(2, fakeRenderer.MeshDraws.Count);
    }

    [Fact(DisplayName = "Render should skip entities marked as Culled")]
    public void Render_SkipsCulledEntities()
    {
        var world = new WorldManager(new SystemManager());
        var system = new MeshRenderingSystem();
        system.Initialize(world);

        var visible = world.CreateEntity();
        world.AddComponent(visible, new Transform(new Vector3(0, 0, 0)));
        world.AddComponent(visible, new Mesh(1, 3));

        var culled = world.CreateEntity();
        world.AddComponent(culled, new Transform(new Vector3(10, 0, 0)));
        world.AddComponent(culled, new Mesh(2, 6));
        world.AddComponent(culled, new Culled());

        var fakeRenderer = new FakeRenderer();
        var context = new FakeRenderContext(fakeRenderer);

        system.Render(context);

        Assert.Single(fakeRenderer.MeshDraws);
    }

    [Fact(DisplayName = "Render should use world matrix for child transforms")]
    public void Render_UsesWorldMatrix_ForChild()
    {
        var world = new WorldManager(new SystemManager());
        var system = new MeshRenderingSystem();
        system.Initialize(world);

        var parent = world.CreateEntity();
        world.AddComponent(parent, new Transform(new Vector3(1, 0, 0)));
        world.AddComponent(parent, new Mesh(1, 6));

        var child = world.CreateEntity();
        var childTransform = new Transform(new Vector3(0, 2, 0)) { Parent = parent };
        world.AddComponent(child, childTransform);
        world.AddComponent(child, new Mesh(2, 6));

        var expectedChildWorld = childTransform.GetWorldMatrix(world);

        var fakeRenderer = new FakeRenderer();
        var context = new FakeRenderContext(fakeRenderer);

        system.Render(context);

        bool found = fakeRenderer.MeshDraws.Any(dc => MatricesEqual(dc.Transform, expectedChildWorld));
        Assert.True(found, "Expected a draw call with child's world matrix");
    }

    [Fact(DisplayName = "Render throws for an unsupported VertexArrayId instead of silently falling back to a cube")]
    public void Render_Throws_ForUnsupportedVertexArrayId()
    {
        var world = new WorldManager(new SystemManager());
        var system = new MeshRenderingSystem();
        system.Initialize(world);

        var entity = world.CreateEntity();
        world.AddComponent(entity, new Transform(Vector3.Zero));
        world.AddComponent(entity, new Mesh(999, 0));

        var fakeRenderer = new FakeRenderer();
        var context = new FakeRenderContext(fakeRenderer);

        Assert.Throws<NotSupportedException>(() => system.Render(context));
    }

    private static bool MatricesEqual(Matrix4x4 a, Matrix4x4 b, float eps = 1e-5f)
    {
        return MathF.Abs(a.M11 - b.M11) < eps &&
               MathF.Abs(a.M12 - b.M12) < eps &&
               MathF.Abs(a.M13 - b.M13) < eps &&
               MathF.Abs(a.M14 - b.M14) < eps &&
               MathF.Abs(a.M21 - b.M21) < eps &&
               MathF.Abs(a.M22 - b.M22) < eps &&
               MathF.Abs(a.M23 - b.M23) < eps &&
               MathF.Abs(a.M24 - b.M24) < eps &&
               MathF.Abs(a.M31 - b.M31) < eps &&
               MathF.Abs(a.M32 - b.M32) < eps &&
               MathF.Abs(a.M33 - b.M33) < eps &&
               MathF.Abs(a.M34 - b.M34) < eps &&
               MathF.Abs(a.M41 - b.M41) < eps &&
               MathF.Abs(a.M42 - b.M42) < eps &&
               MathF.Abs(a.M43 - b.M43) < eps &&
               MathF.Abs(a.M44 - b.M44) < eps;
    }
}
