using System.Numerics;

namespace HEngine.Rendering.Contracts;

public interface IRenderer : IDisposable {
    bool ShouldClose { get; }
    void Initialize(int width, int height, string title);
    void BeginFrame();
    void EndFrame();
    void Clear(Vector4 clearColor);
    void SetViewMatrix(Matrix4x4 viewMatrix);
    void SetProjectionMatrix(Matrix4x4 projectionMatrix);
    void DrawSprite(Vector2 position, Vector2 size, Vector4 color);
    void DrawMesh(Matrix4x4 transform, ReadOnlySpan<float> vertices, ReadOnlySpan<uint> indices);
    void Run();
    void Present();
    void PollEvents();
}