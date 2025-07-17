using HEngine.Rendering.Contracts;
using System.Numerics;

namespace HEngine.Rendering.Renderers;

public interface ISpriteRenderer : IRenderResource {
    void DrawSprite(Vector2 position, Vector2 size, Vector4 color);
}