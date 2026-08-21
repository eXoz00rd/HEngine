using HEngine.Core.Contracts;
using HEngine.Core.Managers;
using HEngine.Core.Primitives;

namespace HEngine.ECS.Queries;

public class Query<T1, T2, T3> : IQuery<T1, T2, T3>
    where T1 : struct, IComponent
    where T2 : struct, IComponent
    where T3 : struct, IComponent
{
    private readonly List<Entity> _cachedEntities = [];
    private readonly ComponentManager _componentManager;
    private readonly EntityManager _entityManager;
    private bool _isDirty = true;

    public Query(ComponentManager componentManager, EntityManager entityManager)
    {
        _componentManager = componentManager ?? throw new ArgumentNullException(nameof(componentManager));
        _entityManager = entityManager ?? throw new ArgumentNullException(nameof(entityManager));
    }

    public int Count
    {
        get
        {
            RefreshIfNeeded();
            return _cachedEntities.Count;
        }
    }

    public bool IsEmpty => Count == 0;

    public void Clear()
    {
        _cachedEntities.Clear();
        _isDirty = true;
    }

    public QueryEnumerator<T1, T2, T3> GetEnumerator()
    {
        RefreshIfNeeded();
        return new QueryEnumerator<T1, T2, T3>(_cachedEntities, _componentManager);
    }

    public bool TryGetFirst(out Entity entity, out T1 component1, out T2 component2, out T3 component3)
    {
        RefreshIfNeeded();

        if (_cachedEntities.Count > 0)
        {
            entity = _cachedEntities[0];
            component1 = _componentManager.GetComponent<T1>(entity);
            component2 = _componentManager.GetComponent<T2>(entity);
            component3 = _componentManager.GetComponent<T3>(entity);
            return true;
        }

        entity = default;
        component1 = default;
        component2 = default;
        component3 = default;
        return false;
    }

    public List<Entity> GetEntities()
    {
        RefreshIfNeeded();
        return [.._cachedEntities];
    }

    private void RefreshIfNeeded()
    {
        if (!_isDirty)
            return;

        _cachedEntities.Clear();
        _componentManager.GetEntitiesWithComponent<T1>(_cachedEntities);

        _cachedEntities.RemoveAll(e => !_componentManager.HasComponent<T2>(e) || !_componentManager.HasComponent<T3>(e)
        );
        _isDirty = false;
    }
}

public class Query<T1, T2> : IQuery<T1, T2>
    where T1 : struct, IComponent
    where T2 : struct, IComponent
{
    private readonly List<Entity> _cachedEntities = [];
    private readonly ComponentManager _componentManager;
    private readonly EntityManager _entityManager;
    private bool _isDirty = true;

    public Query(ComponentManager componentManager, EntityManager entityManager)
    {
        _componentManager = componentManager ?? throw new ArgumentNullException(nameof(componentManager));
        _entityManager = entityManager ?? throw new ArgumentNullException(nameof(entityManager));
    }

    public int Count
    {
        get
        {
            RefreshIfNeeded();
            return _cachedEntities.Count;
        }
    }

    public bool IsEmpty => Count == 0;

    public void Clear()
    {
        _cachedEntities.Clear();
        _isDirty = true;
    }

    public QueryEnumerator<T1, T2> GetEnumerator()
    {
        RefreshIfNeeded();
        return new QueryEnumerator<T1, T2>(_cachedEntities, _componentManager);
    }

    public bool TryGetFirst(out Entity entity, out T1 component1, out T2 component2)
    {
        RefreshIfNeeded();

        if (_cachedEntities.Count > 0)
        {
            entity = _cachedEntities[0];
            component1 = _componentManager.GetComponent<T1>(entity);
            component2 = _componentManager.GetComponent<T2>(entity);
            return true;
        }

        entity = default;
        component1 = default;
        component2 = default;
        return false;
    }

    public List<Entity> GetEntities()
    {
        RefreshIfNeeded();
        return [.._cachedEntities];
    }

    private void RefreshIfNeeded()
    {
        if (!_isDirty)
            return;

        _cachedEntities.Clear();
        _componentManager.GetEntitiesWithComponent<T1>(_cachedEntities);

        _cachedEntities.RemoveAll(e => !_componentManager.HasComponent<T2>(e));
        _isDirty = false;
    }
}

public class Query<T1> : IQuery<T1>
    where T1 : struct, IComponent
{
    private readonly List<Entity> _cachedEntities = [];
    private readonly ComponentManager _componentManager;
    private readonly EntityManager _entityManager;
    private bool _isDirty = true;

    public Query(ComponentManager componentManager, EntityManager entityManager)
    {
        _componentManager = componentManager ?? throw new ArgumentNullException(nameof(componentManager));
        _entityManager = entityManager ?? throw new ArgumentNullException(nameof(entityManager));
    }

    public int Count
    {
        get
        {
            RefreshIfNeeded();
            return _cachedEntities.Count;
        }
    }

    public bool IsEmpty => Count == 0;

    public void Clear()
    {
        _cachedEntities.Clear();
        _isDirty = true;
    }

    public QueryEnumerator<T1> GetEnumerator()
    {
        RefreshIfNeeded();
        return new QueryEnumerator<T1>(_cachedEntities, _componentManager);
    }

    public bool TryGetFirst(out Entity entity, out T1 component1)
    {
        RefreshIfNeeded();

        if (_cachedEntities.Count > 0)
        {
            entity = _cachedEntities[0];
            component1 = _componentManager.GetComponent<T1>(entity);
            return true;
        }

        entity = default;
        component1 = default;
        return false;
    }

    public List<Entity> GetEntities()
    {
        RefreshIfNeeded();
        return [.._cachedEntities];
    }

    private void RefreshIfNeeded()
    {
        if (!_isDirty)
            return;

        _cachedEntities.Clear();
        _componentManager.GetEntitiesWithComponent<T1>(_cachedEntities);
        _isDirty = false;
    }
}