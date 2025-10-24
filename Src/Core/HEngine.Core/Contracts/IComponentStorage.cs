using HEngine.Core.Primitives;

namespace HEngine.Core.Contracts;

internal interface IComponentStorage {
    uint Count { get; }

    Type ComponentType { get; }
    
    bool RemoveEntity(Entity entity);
    
    bool HasEntity(Entity entity);
    
    void GetAllEntities(List<Entity> entities);
    
    void Clear();
    
    void Compact();
    
    long GetMemoryUsage();
}

internal interface IComponentStorage<T> : IComponentStorage where T : struct, IComponent {
    ref T AddComponent(Entity entity, in T component);
    
    bool RemoveComponent(Entity entity);

    bool HasComponent(Entity entity);

    ref T GetComponent(Entity entity);

   bool TryGetComponent(Entity entity, out T component);

    Span<T> GetAllComponents();

   void GetEntitiesWithComponent(List<Entity> entities);
}