using HEngine.Core.Rendering.Contracts;

namespace HEngine.Rendering.Factories;

public interface IRenderContextFactory
{
    IRenderContext Create();
}

public sealed class SilkRenderContextFactory : IRenderContextFactory
{
    private readonly IRenderer _renderer;

    public SilkRenderContextFactory(IRenderer renderer)
    {
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
    }

    public IRenderContext Create()
    {
        return new SilkRenderContext(_renderer);
    }
}