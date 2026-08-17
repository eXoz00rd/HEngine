using HEngine.Core.Contracts;
using HEngine.Core.Primitives;
using HEngine.Core.Queries;

namespace HEngine.Core.Managers;

public class WorldManager : IDisposable {
    private readonly List<IQuery> _queryCache = new();
    private readonly Dictionary<Type, ISystem> _systemCache = new();
    private readonly SystemManager _systemManager;
    private bool _disposed;

    public WorldManager(SystemManager systemManager)
    {
        _systemManager = systemManager ?? throw new ArgumentNullException(nameof(systemManager));
        EntityManager = new EntityManager();
        ComponentManager = new ComponentManager(EntityManager);
        QueryBuilder = new QueryBuilder(ComponentManager, EntityManager);
    }

    public EntityManager EntityManager { get; }
    public ComponentManager ComponentManager { get; }
    public QueryBuilder QueryBuilder { get; }

    public void Dispose()
    {
        if (_disposed)
            return;

        _systemManager.Dispose();
        _systemCache.Clear();
        _queryCache.Clear();
        ComponentManager.Dispose();
        EntityManager.Dispose();
        _disposed = true;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(WorldManager));
    }

    public Query<T1> CreateQuery<T1>() where T1 : struct, IComponent
    {
        var query = QueryBuilder.With<T1>();
        _queryCache.Add(query);
        return query;
    }

    public Query<T1, T2> CreateQuery<T1, T2>()
        where T1 : struct, IComponent
        where T2 : struct, IComponent
    {
        var query = QueryBuilder.With<T1, T2>();
        _queryCache.Add(query);
        return query;
    }

    public Query<T1, T2, T3> CreateQuery<T1, T2, T3>()
        where T1 : struct, IComponent
        where T2 : struct, IComponent
        where T3 : struct, IComponent
    {
        var query = QueryBuilder.With<T1, T2, T3>();
        _queryCache.Add(query);
        return query;
    }

    private void InvalidateQueries()
    {
        foreach (var query in _queryCache)
            query.Clear();
    }

    public Entity CreateEntity()
    {
        ThrowIfDisposed();
        return EntityManager.CreateEntity();
    }


    public void DestroyEntity(Entity entity)
    {
        if (!EntityManager.IsEntityValid(entity))
            return;

        ComponentManager.RemoveAllComponents(entity);
        EntityManager.DestroyEntity(entity);
        InvalidateQueries();
    }

    public ref T AddComponent<T>(Entity entity, in T component) where T : struct, IComponent
    {
        if (!EntityManager.IsEntityValid(entity))
            throw new ArgumentException($"Invalid entity: {entity}", nameof(entity));

        ref var result = ref ComponentManager.AddComponent(entity, in component);
        InvalidateQueries();
        return ref result;
    }

    public ref T SetComponent<T>(Entity entity, in T component) where T : struct, IComponent
    {
        if (!EntityManager.IsEntityValid(entity))
            throw new ArgumentException($"Invalid entity: {entity}", nameof(entity));

        if (ComponentManager.HasComponent<T>(entity))
        {
            ref var existingComponent = ref ComponentManager.GetComponent<T>(entity);
            existingComponent = component;
            return ref existingComponent;
        }

        ref var result = ref ComponentManager.AddComponent(entity, in component);
        InvalidateQueries();
        return ref result;
    }


    public bool RemoveComponent<T>(Entity entity) where T : struct, IComponent
    {
        if (!EntityManager.IsEntityValid(entity))
            return false;

        var result = ComponentManager.RemoveComponent<T>(entity);
        if (result)
            InvalidateQueries();
        return result;
    }

    public ref T GetComponent<T>(Entity entity) where T : struct, IComponent
    {
        if (!EntityManager.IsEntityValid(entity))
            throw new ArgumentException($"Invalid entity: {entity}", nameof(entity));

        return ref ComponentManager.GetComponent<T>(entity);
    }

    public bool HasComponent<T>(Entity entity) where T : struct, IComponent
        => EntityManager.IsEntityValid(entity) && ComponentManager.HasComponent<T>(entity);

    public bool TryGetComponent<T>(Entity entity, out T component) where T : struct, IComponent
    {
        if (EntityManager.IsEntityValid(entity))
            return ComponentManager.TryGetComponent(entity, out component);

        component = default;
        return false;
    }

    public void AddSystem<T>(T system, int priority = 0, bool enabled = true) where T : ISystem
    {
        var systemType = typeof(T);

        if (_systemCache.ContainsKey(systemType))
            throw new InvalidOperationException($"System of type {systemType.Name} already exists");

        _systemManager.AddSystem(system, priority, enabled);
        _systemCache[systemType] = system;

        system.Initialize(this);
    }

    public void RemoveSystem<T>() where T : ISystem
    {
        var systemType = typeof(T);
        _systemManager.RemoveSystem<T>();
        _systemCache.Remove(systemType);
    }

    public void SetSystemEnabled<T>(bool enabled) where T : ISystem
        => _systemManager.SetSystemEnabled<T>(enabled);

    public T? GetSystem<T>() where T : class, ISystem
    {
        var systemType = typeof(T);
        return _systemCache.TryGetValue(systemType, out var system) ?
            system as T :
            null;
    }

    public bool HasSystem<T>() where T : ISystem
        => _systemCache.ContainsKey(typeof(T));

    public void Update(float deltaTime)
        => _systemManager.Update(deltaTime);

    public int GetEntityCount()
        => EntityManager.ActiveEntityCount;

    public int GetSystemCount()
        => _systemManager.GetSystemCount();

    public int GetActiveSystemCount()
        => _systemManager.GetActiveSystemCount();

    public int GetComponentCount<T>() where T : struct, IComponent
        => ComponentManager.GetComponentCount<T>();

    public void DestroyEntities(ReadOnlySpan<Entity> entities)
    {
        foreach (var entity in entities)
            DestroyEntity(entity);
    }

    public void AddComponents<T>(ReadOnlySpan<(Entity, T)> components) where T : struct, IComponent
    {
        foreach (var (entity, component) in components)
        {
            if (EntityManager.IsEntityValid(entity))
                ComponentManager.AddComponent(entity, in component);
        }

        InvalidateQueries();
    }
}