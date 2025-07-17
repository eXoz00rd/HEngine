using HEngine.Core.Contracts;
using HEngine.Core.Managers;
using HEngine.Core.Primitives;

namespace HEngine.Core.Queries;

public readonly ref struct QueryItem<T1, T2, T3>
    where T1 : struct, IComponent
    where T2 : struct, IComponent
    where T3 : struct, IComponent {
    private readonly ComponentManager _componentManager;

    public QueryItem(Entity entity, ComponentManager componentManager)
    {
        Entity = entity;
        _componentManager = componentManager;
    }

    public Entity Entity { get; }

    public ref T1 Component1 => ref _componentManager.GetComponent<T1>(Entity);
    public ref T2 Component2 => ref _componentManager.GetComponent<T2>(Entity);
    public ref T3 Component3 => ref _componentManager.GetComponent<T3>(Entity);

    public void Deconstruct(out Entity entity, out T1 component1, out T2 component2, out T3 component3)
    {
        entity = Entity;
        component1 = _componentManager.GetComponent<T1>(Entity);
        component2 = _componentManager.GetComponent<T2>(Entity);
        component3 = _componentManager.GetComponent<T3>(Entity);
    }
}

public readonly ref struct QueryItem<T1, T2>
    where T1 : struct, IComponent
    where T2 : struct, IComponent {
    private readonly ComponentManager _componentManager;

    public QueryItem(Entity entity, ComponentManager componentManager)
    {
        Entity = entity;
        _componentManager = componentManager;
    }

    public Entity Entity { get; }

    public ref T1 Component1 => ref _componentManager.GetComponent<T1>(Entity);
    public ref T2 Component2 => ref _componentManager.GetComponent<T2>(Entity);

    public void Deconstruct(out Entity entity, out T1 component1, out T2 component2)
    {
        entity = Entity;
        component1 = _componentManager.GetComponent<T1>(Entity);
        component2 = _componentManager.GetComponent<T2>(Entity);
    }
}

public readonly ref struct QueryItem<T1>
    where T1 : struct, IComponent {
    private readonly ComponentManager _componentManager;

    public QueryItem(Entity entity, ComponentManager componentManager)
    {
        Entity = entity;
        _componentManager = componentManager;
    }

    public Entity Entity { get; }

    public ref T1 Component1 => ref _componentManager.GetComponent<T1>(Entity);

    public void Deconstruct(out Entity entity, out T1 component1)
    {
        entity = Entity;
        component1 = _componentManager.GetComponent<T1>(Entity);
    }
}