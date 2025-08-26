using HEngine.Core.Managers;
using HEngine.Core.Rendering.Contracts;

namespace HEngine.Rendering.Systems.Contracts;

public interface ISpriteRenderingSystem : IDisposable
{
    void Initialize(WorldManager worldManager);
    void Render(IRenderContext context);
}