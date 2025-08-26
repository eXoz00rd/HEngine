using System.Numerics;

namespace HEngine.Core.Rendering.DirectX12.Contracts;

public interface IDirectX12SpriteRenderer : IDisposable
{
    void Initialize(IDirectX12Device device);
    void DrawSprite(Vector2 position, Vector2 size, Vector4 color);
    void FlushBatch();
}