using System.Numerics;

namespace HEngine.Rendering.Contracts;

public interface IRenderContext
{
    IRenderer Renderer { get; }
    Matrix4x4 ViewMatrix { get; set; }
    Matrix4x4 ProjectionMatrix { get; set; }
    Vector4 ClearColor { get; set; }
}
