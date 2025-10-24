using System.Numerics;

namespace HEngine.Core.Rendering.Contracts;

public interface ISpriteRenderer : IDisposable
{
    bool IsInitialized { get; }
    void Initialize(IGraphicsDevice device);
    void DrawSprite(Vector2 position, Vector2 size, Vector4 color);
    void UpdateCameraMatrices(Matrix4x4 view, Matrix4x4 projection);
    void FlushBatch();
}