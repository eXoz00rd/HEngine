using System;
using System.Numerics;
using HEngine.Core.Rendering.Contracts;
using HEngine.Core.Rendering.Data;
using HEngine.Rendering.Renderers;
using Xunit;

namespace HEngine.Rendering.Tests.Renderers
{
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

    file sealed class NullRenderer : IRenderer
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
        public void DrawSprite(Vector2 position, Vector2 size, Vector4 color) { }
        public void FlushBatch() { }
        public void DrawMesh(Matrix4x4 transform, ReadOnlySpan<float> vertices, ReadOnlySpan<uint> indices) { }
        public void Run() { }
        public void Dispose() { }
    }

    public class DirectX12MeshRendererTests
    {
        [Fact(DisplayName = "DirectX12MeshRenderer initializes and toggles pipeline flags")]
        public void Initialize_And_Toggle_Flags()
        {
            var renderer = new DirectX12MeshRenderer();
            renderer.Initialize();

            Assert.True(renderer.IsInitialized);
            Assert.True(renderer.DepthTestEnabled);
            Assert.True(renderer.BackFaceCullingEnabled);

            renderer.SetDepthTest(false);
            renderer.SetBackFaceCulling(false);

            Assert.False(renderer.DepthTestEnabled);
            Assert.False(renderer.BackFaceCullingEnabled);
        }

        [Fact(DisplayName = "DrawMesh computes MVP and records vertex/index counts for cube")]
        public void DrawMesh_Computes_Mvp_And_Records_Counts()
        {
            var renderer = new DirectX12MeshRenderer();
            renderer.Initialize();

            var nullRenderer = new NullRenderer();
            var context = new FakeRenderContext(nullRenderer)
            {
                ViewMatrix = Matrix4x4.CreateLookAt(new Vector3(0,0,-5), Vector3.Zero, Vector3.UnitY),
                ProjectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI/4f, 16f/9f, 0.1f, 100f)
            };
            
            var model = Matrix4x4.CreateTranslation(1, 2, 3);

            var (verts, indices) = MeshPrimitives.CreateCube(1.0f);

            renderer.DrawMesh(model, verts, indices, context);
            
            Assert.Equal(24, renderer.LastDrawVertexCount);
            Assert.Equal(36, renderer.LastDrawIndexCount);
            
            Assert.False(Matrix4x4.Identity.Equals(renderer.LastMvp));
        }

        [Fact(DisplayName = "Multiple DrawMesh calls update last draw metadata")]
        public void Multiple_DrawMesh_Calls_Update_Metadata()
        {
            var renderer = new DirectX12MeshRenderer();
            renderer.Initialize();

            var nullRenderer = new NullRenderer();
            var context = new FakeRenderContext(nullRenderer);

            var (verts1, indices1) = MeshPrimitives.CreateCube(1.0f);
            var model1 = Matrix4x4.Identity;
            renderer.DrawMesh(model1, verts1, indices1, context);

            Assert.Equal(24, renderer.LastDrawVertexCount);
            Assert.Equal(36, renderer.LastDrawIndexCount);

            var (verts2, indices2) = MeshPrimitives.CreatePlane(2.0f, 3.0f);
            var model2 = Matrix4x4.CreateTranslation(5, 0, 0);
            renderer.DrawMesh(model2, verts2, indices2, context);

            Assert.Equal(4, renderer.LastDrawVertexCount);
            Assert.Equal(6, renderer.LastDrawIndexCount);
        }
    }
}
