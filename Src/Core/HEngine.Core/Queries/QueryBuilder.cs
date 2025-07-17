using HEngine.Core.Contracts;
using HEngine.Core.Managers;

namespace HEngine.Core.Queries;

public class QueryBuilder {
    private readonly ComponentManager _componentManager;
    private readonly EntityManager _entityManager;

    public QueryBuilder(ComponentManager componentManager, EntityManager entityManager)
    {
        _componentManager = componentManager ?? throw new ArgumentNullException(nameof(componentManager));
        _entityManager = entityManager ?? throw new ArgumentNullException(nameof(entityManager));
    }

    public Query<T1> With<T1>() where T1 : struct, IComponent
        => new(_componentManager, _entityManager);

    public Query<T1, T2> With<T1, T2>()
        where T1 : struct, IComponent
        where T2 : struct, IComponent
        => new(_componentManager, _entityManager);

    public Query<T1, T2, T3> With<T1, T2, T3>()
        where T1 : struct, IComponent
        where T2 : struct, IComponent
        where T3 : struct, IComponent
        => new(_componentManager, _entityManager);
}