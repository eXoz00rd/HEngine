using HEngine.Core.Managers;

namespace HEngine.ECS.Contracts;

public interface ISystemRegistration
{
    void AddTo(WorldManager world);
}
