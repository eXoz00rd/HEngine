using System;
using System.Numerics;
using HEngine.Core.Components.Rendering;
using HEngine.Core.Configuration;
using HEngine.Core.Managers;
using HEngine.Core.Rendering.Contracts;
using HEngine.Rendering;
using HEngine.Rendering.PostProcessing;
using HEngine.Rendering.Systems;
using Xunit;

namespace HEngine.Rendering.Tests
{
    file sealed class FakeRenderer : IRenderer
    {
        public bool IsInitialized { get; private set; }
        public bool ShouldClose { get; private set; }

        public Matrix4x4? LastViewMatrix { get; private set; }
        public Matrix4x4? LastProjectionMatrix { get; private set; }

        public void Initialize(int width, int height, string title)
        {
            IsInitialized = true;
        }

        public void BeginFrame() { }
        public void EndFrame() { }
        public void Clear(Vector4 clearColor) { }
        public void Present() { }
        public void PollEvents() { }

        public void SetViewMatrix(Matrix4x4 viewMatrix)
        {
            LastViewMatrix = viewMatrix;
        }

        public void SetProjectionMatrix(Matrix4x4 projectionMatrix)
        {
            LastProjectionMatrix = projectionMatrix;
        }

        public void DrawSprite(Vector2 position, Vector2 size, Vector4 color) { }

        public void FlushBatch() { }

        public void DrawMesh(Matrix4x4 transform, ReadOnlySpan<float> vertices, ReadOnlySpan<uint> indices) { }

        public void Run() { }

        public void Dispose() { }
    }

    file sealed class FakeRenderContext : IRenderContext
    {
        public IRenderer Renderer { get; }
        public Matrix4x4 ViewMatrix { get; set; } = Matrix4x4.Identity;
        public Matrix4x4 ProjectionMatrix { get; set; } = Matrix4x4.Identity;
        public Vector4 ClearColor { get; set; } = new(0, 0, 0, 1);

        public FakeRenderContext(IRenderer renderer) => Renderer = renderer;
    }

    file sealed class FakeRenderManager : IRenderManager
    {
        private readonly IRenderContext _context;

        public FakeRenderManager(IRenderContext context)
        {
            _context = context;
        }

        public bool ShouldClose => false;
        public bool CanRender => true;
        public bool IsInitialized => true;

        public void Initialize(int width, int height, string title) { }
        public void UpdateInput() { }
        public void BeginRender() { }
        public void EndRender() { }
        public void Clear(Vector4 clearColor) { }
        public void Present() { }

        public IRenderContext GetRenderContext() => _context;
        public bool TryGetRenderContext(out IRenderContext context)
        {
            context = _context;
            return true;
        }

        public void SetActiveCamera(ICamera camera) { }
        public bool TryGetActiveCamera(out ICamera camera)
        {
            camera = default!;
            return false;
        }

        public void Dispose() { }
    }

    file sealed class FakeRenderingSystem : IRenderingSystem
    {
        public bool RenderCalled { get; private set; }
        public bool IsInitialized { get; private set; }

        public void Initialize(WorldManager worldManager)
        {
            IsInitialized = true;
        }

        public void Render(IRenderContext context)
        {
            RenderCalled = true;
        }

        public void Update(float deltaTime) { }

        public void Dispose() { }
    }

    file sealed class NullLogger<T> : Microsoft.Extensions.Logging.ILogger<T>
    {
        public IDisposable BeginScope<TState>(TState state) => NullDisposable.Instance;
        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => false;
        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) { }

        private sealed class NullDisposable : IDisposable
        {
            public static readonly NullDisposable Instance = new();
            public void Dispose() { }
        }
    }

    public class RenderPipelineTests
    {
        [Fact(DisplayName = "RenderPipeline uses ECS Camera view/projection matrices")]
        public void RenderPipeline_Uses_ECS_Camera_Matrices()
        {
            var world = new WorldManager();
            var e = world.CreateEntity();
            var cam = new Camera
            {
                FieldOfView = MathF.PI / 3f,
                NearPlane = 0.5f,
                FarPlane = 500f,
                AspectRatio = 16f / 9f,
                IsOrthographic = false,
                Position = new Vector3(3, 4, 5),
                Target = new Vector3(0, 0, 0),
                Up = Vector3.UnitY
            };
            world.AddComponent(e, cam);
            
            var fakeRenderer = new FakeRenderer();
            var context = new FakeRenderContext(fakeRenderer);
            var renderManager = new FakeRenderManager(context);
            var renderingSystem = new FakeRenderingSystem();
            var logger = new NullLogger<RenderPipeline>();

            var lightingSystem = new LightingSystem();
            lightingSystem.Initialize(world);
            var shadowRenderingSystem = new ShadowRenderingSystem();
            shadowRenderingSystem.Initialize(world);
            var shadowSettings = new ShadowSettings { Enabled = false };
            var postProcessStack = new PostProcessStack();

            var pipeline = new RenderPipeline(
                renderManager,
                renderingSystem,
                world,
                lightingSystem,
                shadowRenderingSystem,
                shadowSettings,
                postProcessStack,
                logger);
            
            pipeline.RenderFrame();
            
            var expectedView = cam.GetViewMatrix();
            var expectedProj = cam.GetProjectionMatrix();

            Assert.True(fakeRenderer.LastViewMatrix.HasValue, "View matrix was not set on renderer");
            Assert.True(fakeRenderer.LastProjectionMatrix.HasValue, "Projection matrix was not set on renderer");

            Assert.True(MatricesEqual(expectedView, fakeRenderer.LastViewMatrix!.Value));
            Assert.True(MatricesEqual(expectedProj, fakeRenderer.LastProjectionMatrix!.Value));
            
            Assert.True(renderingSystem.RenderCalled);
        }

        private static bool MatricesEqual(in Matrix4x4 a, in Matrix4x4 b, float eps = 1e-5f)
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
}
