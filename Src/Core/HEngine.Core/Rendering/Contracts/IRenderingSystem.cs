using HEngine.Core.Contracts;

namespace HEngine.Core.Rendering.Contracts;

public interface IRenderingSystem : ISystem
{
    bool IsInitialized { get; }
    void Render(IRenderContext context);
}