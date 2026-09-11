using HEngine.Core.Rendering.Contracts;

namespace HEngine.Rendering.Factories;

public interface IRenderContextFactory
{
    IRenderContext Create();
}
