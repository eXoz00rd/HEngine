using HEngine.Core.Contracts;
using HEngine.Core.Managers;
using HEngine.Core.Primitives;

namespace HEngine.Core;

public static class QueryExtensions {
    public static void ForEach<T>(this WorldManager world, Action<Entity, T> action)
        where T : struct, IComponent
    {
        var entities = new List<Entity>();
        world.ComponentManager.GetEntitiesWithComponent<T>(entities);

        foreach (var entity in entities)
        {
            var component = world.ComponentManager.GetComponent<T>(entity);
            action(entity, component);
        }
    }
}