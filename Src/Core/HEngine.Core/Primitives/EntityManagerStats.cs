namespace HEngine.Core.Primitives;

public readonly struct EntityManagerStats {
    public int ActiveCount { get; init; }
    public uint TotalCreated { get; init; }
    public int RecycledCount { get; init; }
    public uint NextEntityId { get; init; }
    public int GenerationsCount { get; init; }

    public override string ToString()
        => $"EntityManager Stats: Active={ActiveCount}, Total={TotalCreated}, " +
            $"Recycled={RecycledCount}, Next={NextEntityId}, Generations={GenerationsCount}";
}