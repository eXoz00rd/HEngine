using HEngine.Core.Managers;

namespace HEngine.Core.Contracts;

public interface ISystem : IDisposable
{
    void Initialize(WorldManager worldManager);
    void Update(float deltaTime);
}