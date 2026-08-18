using System.Numerics;
using HEngine.Core.Rendering.Contracts;
using HEngine.Core.Rendering.Data;
using HEngine.Rendering.Data;
using HEngine.Rendering.Renderers;

namespace HEngine.Rendering.Tests.Renderers;

file sealed class FakePBRContext : IRenderContext
{
    public FakePBRContext() { }

    public IRenderer Renderer { get; } = new NullPBRRenderer();
    public Matrix4x4 ViewMatrix { get; set; } = Matrix4x4.Identity;
    public Matrix4x4 ProjectionMatrix { get; set; } = Matrix4x4.Identity;
    public Vector4 ClearColor { get; set; } = new(0, 0, 0, 1);
}

file sealed class NullPBRRenderer : IRenderer
{
    public bool IsInitialized => true;
    public bool ShouldClose => false;

    public void Initialize(int width, int height, string title) { }
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
    public void DrawMesh(Matrix4x4 transform, ReadOnlySpan<float> vertices, ReadOnlySpan<uint> indices) { }
    public void Run() { }
    public void Dispose() { }
}

public class PBRMeshRendererTests
{
    [Fact(DisplayName = "DrawMesh with material computes MVP correctly")]
    public void DrawMesh_With_Material_Computes_MVP()
    {
        var renderer = new DirectX12MeshRenderer();
        renderer.Initialize();

        var context = new FakePBRContext
        {
            ViewMatrix = Matrix4x4.CreateLookAt(new Vector3(0, 0, -5), Vector3.Zero, Vector3.UnitY),
            ProjectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 4f, 16f / 9f, 0.1f, 100f)
        };

        var material = new Material { Metallic = 1.0f, Roughness = 0.2f };
        var model = Matrix4x4.CreateTranslation(1, 2, 3);

        var (verts, indices) = MeshPrimitives.CreateCube();

        renderer.DrawMesh(model, verts, indices, context, material);

        Assert.Equal(24, renderer.LastDrawVertexCount);
        Assert.Equal(36, renderer.LastDrawIndexCount);
        Assert.NotEqual(Matrix4x4.Identity, renderer.LastMvp);
    }

    [Fact(DisplayName = "DrawMesh with 8 lights records counts correctly")]
    public void DrawMesh_With_Eight_Lights()
    {
        var renderer = new DirectX12MeshRenderer();
        renderer.Initialize();

        var context = new FakePBRContext();
        var (verts, indices) = MeshPrimitives.CreateCube();

        var lights = new LightData[8];
        for (int i = 0; i < 8; i++)
        {
            lights[i] = new LightData
            {
                Type = LightType.Point,
                Color = Vector3.One,
                Intensity = 1f,
                Position = new Vector3(i, 0, 0),
                Range = 10f
            };
        }

        renderer.DrawMesh(Matrix4x4.Identity, verts, indices, context, null, lights);

        Assert.Equal(36, renderer.LastDrawIndexCount);
    }

    [Fact(DisplayName = "DrawMesh backward compatible without material and lights")]
    public void DrawMesh_BackwardCompatible_Without_Material()
    {
        var renderer = new DirectX12MeshRenderer();
        renderer.Initialize();

        var context = new FakePBRContext();
        var (verts, indices) = MeshPrimitives.CreateCube();

        renderer.DrawMesh(Matrix4x4.Identity, verts, indices, context);

        Assert.Equal(24, renderer.LastDrawVertexCount);
    }
}
