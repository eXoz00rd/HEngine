using HEngine.Core.Contracts;
using HEngine.Core.Managers;
using HEngine.Core.Primitives;

namespace HEngine.ECS.Queries;

public ref struct QueryEnumerator<T1, T2, T3>
    where T1 : struct, IComponent
    where T2 : struct, IComponent
    where T3 : struct, IComponent {
    private readonly List<Entity> _entities;
    private readonly ComponentManager _componentManager;
    private int _index;

    public QueryEnumerator(List<Entity> entities, ComponentManager componentManager)
    {
        _entities = entities;
        _componentManager = componentManager;
        _index = -1;
    }

    public bool MoveNext()
        => ++_index < _entities.Count;

    public readonly QueryItem<T1, T2, T3> Current
    {
        get
        {
            var entity = _entities[_index];
            return new QueryItem<T1, T2, T3>(entity, _componentManager);
        }
    }
}

public ref struct QueryEnumerator<T1, T2>
    where T1 : struct, IComponent
    where T2 : struct, IComponent {
    private readonly List<Entity> _entities;
    private readonly ComponentManager _componentManager;
    private int _index;

    public QueryEnumerator(List<Entity> entities, ComponentManager componentManager)
    {
        _entities = entities;
        _componentManager = componentManager;
        _index = -1;
    }

    public bool MoveNext()
        => ++_index < _entities.Count;

    public readonly QueryItem<T1, T2> Current
    {
        get
        {
            var entity = _entities[_index];
            return new QueryItem<T1, T2>(entity, _componentManager);
        }
    }
}

public ref struct QueryEnumerator<T1>
    where T1 : struct, IComponent {
    private readonly List<Entity> _entities;
    private readonly ComponentManager _componentManager;
    private int _index;

    public QueryEnumerator(List<Entity> entities, ComponentManager componentManager)
    {
        _entities = entities;
        _componentManager = componentManager;
        _index = -1;
    }

    public bool MoveNext()
        => ++_index < _entities.Count;

    public readonly QueryItem<T1> Current
    {
        get
        {
            var entity = _entities[_index];
            return new QueryItem<T1>(entity, _componentManager);
        }
    }
}