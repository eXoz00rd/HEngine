using HEngine.Core.Contracts;
using HEngine.Core.Primitives;
using HEngine.Core.Storages;
using System.Collections.Concurrent;

namespace HEngine.Core.Managers;

public sealed class ComponentManager : IDisposable {
    private readonly ConcurrentDictionary<Type, object> _componentStorages = new();
    private readonly EntityManager _entityManager;
    private readonly Lock _lock = new();
    private bool _disposed;

    public ComponentManager(EntityManager entityManager)
    {
        _entityManager = entityManager ?? throw new ArgumentNullException(nameof(entityManager));
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        lock (_lock)
        {
            if (_disposed)
                return;
            
            foreach (var storage in _componentStorages.Values)
            {
                if (storage is IDisposable disposableStorage)
                    disposableStorage.Dispose();
            }

            _componentStorages.Clear();
            _disposed = true;
        }
    }

    public ref T AddComponent<T>(Entity entity, in T component) where T : struct, IComponent
    {
        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (!_entityManager.IsEntityValid(entity))
                throw new ArgumentException($"Invalid entity: {entity}", nameof(entity));

            var storage = GetOrCreateStorage<T>();
            return ref storage.AddComponent(entity, component);
        }
    }

    public bool RemoveComponent<T>(Entity entity) where T : struct, IComponent
    {
        lock (_lock)
        {
            if (_disposed || !_entityManager.IsEntityValid(entity))
                return false;

            var storage = GetStorage<T>();
            return storage?.RemoveComponent(entity) ?? false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasComponent<T>(Entity entity) where T : struct, IComponent
    {
        lock (_lock)
        {
            if (_disposed || !_entityManager.IsEntityValid(entity))
                return false;

            var storage = GetStorage<T>();
            return storage?.HasComponent(entity) ?? false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T GetComponent<T>(Entity entity) where T : struct, IComponent
    {
        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (!_entityManager.IsEntityValid(entity))
                throw new ArgumentException($"Invalid entity: {entity}", nameof(entity));

            var storage = GetStorage<T>();
            if (storage == null)
                throw new InvalidOperationException($"No storage found for component type {typeof(T).Name}");

            return ref storage.GetComponent(entity);
        }
    }

    public bool TryGetComponent<T>(Entity entity, out T component) where T : struct, IComponent
    {
        component = default;

        lock (_lock)
        {
            if (_disposed || !_entityManager.IsEntityValid(entity))
                return false;

            var storage = GetStorage<T>();
            return storage?.TryGetComponent(entity, out component) ?? false;
        }
    }

    public Span<T> GetAllComponents<T>() where T : struct, IComponent
    {
        lock (_lock)
        {
            if (_disposed)
                return Span<T>.Empty;

            var storage = GetStorage<T>();
            return storage != null ?
                storage.GetAllComponents() :
                Span<T>.Empty;
        }
    }

    public void GetEntitiesWithComponent<T>(List<Entity> entities) where T : struct, IComponent
    {
        lock (_lock)
        {
            if (_disposed)
            {
                entities.Clear();
                return;
            }

            var storage = GetStorage<T>();
            if (storage != null)
                storage.GetEntitiesWithComponent(entities);
            else
                entities.Clear();
        }
    }

    public void RemoveAllComponents(Entity entity)
    {
        lock (_lock)
        {
            if (_disposed || !_entityManager.IsEntityValid(entity))
                return;

            foreach (var storage in _componentStorages.Values)
            {
                if (storage is IComponentStorage componentStorage)
                    componentStorage.RemoveEntity(entity);
            }
        }
    }

    private ComponentStorage<T> GetOrCreateStorage<T>() where T : struct, IComponent
        => (ComponentStorage<T>)_componentStorages.GetOrAdd(
            typeof(T),
            _ => new ComponentStorage<T>()
        );

    private ComponentStorage<T>? GetStorage<T>() where T : struct, IComponent
        => _componentStorages.TryGetValue(typeof(T), out var storage) ?
            (ComponentStorage<T>)storage :
            null;

    public int GetComponentCount<T>() where T : struct, IComponent
    {
        lock (_lock)
        {
            if (_disposed)
                return 0;

            var storage = GetStorage<T>();
            return (int)(storage?.Count ?? 0);
        }
    }

    public void QueryComponents<T1, T2>(List<(Entity, T1, T2)> results)
        where T1 : struct, IComponent
        where T2 : struct, IComponent
    {
        results.Clear();

        lock (_lock)
        {
            if (_disposed)
                return;

            var storage1 = GetStorage<T1>();
            var storage2 = GetStorage<T2>();

            if (storage1 == null || storage2 == null)
                return;

            var entities = new List<Entity>();
            storage1.GetEntitiesWithComponent(entities);

            results.AddRange(
                from entity in entities
                where storage2.HasComponent(entity)
                select (entity, storage1.GetComponent(entity), storage2.GetComponent(entity))
            );
        }
    }

    public void AddComponents<T>(ReadOnlySpan<(Entity, T)> components) where T : struct, IComponent
    {
        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            var storage = GetOrCreateStorage<T>();
            foreach (var (entity, component) in components)
                storage.AddComponent(entity, component);
        }
    }
}