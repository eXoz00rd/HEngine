using HEngine.Core.Managers;
using HEngine.Core.Rendering.Contracts;

namespace HEngine.Rendering.Systems;

public interface IMeshRenderingSystem : IDisposable
{
    void Initialize(WorldManager worldManager);
    void Render(IRenderContext context);
}