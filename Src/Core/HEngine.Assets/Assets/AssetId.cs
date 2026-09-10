namespace HEngine.Assets.Assets;

public readonly struct AssetId : IEquatable<AssetId>
{
    private readonly Guid _value;

    public AssetId(Guid value)
    {
        _value = value;
    }

    public static AssetId New() => new(Guid.NewGuid());

    public bool Equals(AssetId other) => _value.Equals(other._value);

    public override bool Equals(object? obj) => obj is AssetId other && Equals(other);

    public override int GetHashCode() => _value.GetHashCode();

    public override string ToString() => _value.ToString();

    public static bool operator ==(AssetId left, AssetId right) => left.Equals(right);

    public static bool operator !=(AssetId left, AssetId right) => !left.Equals(right);
}
