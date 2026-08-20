namespace HEngine.Core.Primitives;

public readonly struct Entity : IEquatable<Entity> {
    public static readonly Entity Null = new(0, 0);

    internal Entity(uint id, uint generation)
    {
        Id = id;
        Generation = generation;
    }
    
    public Entity(uint id) : this(id, 1)
    {
    }

    public uint Id { get; }

    public uint Generation { get; }

    public bool IsValid => Id != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Entity other)
        => Id == other.Id && Generation == other.Generation;

    public override bool Equals(object? obj)
        => obj is Entity other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(Id, Generation);

    public override string ToString()
        => $"Entity({Id}:{Generation})";

    public static bool operator ==(Entity left, Entity right)
        => left.Equals(right);

    public static bool operator !=(Entity left, Entity right)
        => !left.Equals(right);
}