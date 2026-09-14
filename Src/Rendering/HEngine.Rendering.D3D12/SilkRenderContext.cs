using System.Numerics;
using HEngine.Core.Rendering.Contracts;

namespace HEngine.Rendering;

public class SilkRenderContext : IRenderContext
{
    public SilkRenderContext(IRenderer renderer)
    {
        Renderer = renderer;
    }

    public IRenderer Renderer { get; }
    public Matrix4x4 ViewMatrix { get; set; } = Matrix4x4.Identity;
    public Matrix4x4 ProjectionMatrix { get; set; } = Matrix4x4.Identity;
    public Vector4 ClearColor { get; set; } = new(0.1f, 0.1f, 0.1f, 1.0f);
}