using HEngine.Core.Contracts;
using HEngine.Core.Primitives;

namespace HEngine.Core.Queries;

public interface IQuery<T1, T2, T3> : IQuery
    where T1 : struct, IComponent
    where T2 : struct, IComponent
    where T3 : struct, IComponent {
    QueryEnumerator<T1, T2, T3> GetEnumerator();
    bool TryGetFirst(out Entity entity, out T1 component1, out T2 component2, out T3 component3);
}

public interface IQuery<T1, T2> : IQuery
    where T1 : struct, IComponent
    where T2 : struct, IComponent {
    QueryEnumerator<T1, T2> GetEnumerator();
    bool TryGetFirst(out Entity entity, out T1 component1, out T2 component2);
}

public interface IQuery<T1> : IQuery
    where T1 : struct, IComponent {
    QueryEnumerator<T1> GetEnumerator();
    bool TryGetFirst(out Entity entity, out T1 component1);
}

public interface IQuery {
    int Count { get; }
    bool IsEmpty { get; }
    void Clear();
}