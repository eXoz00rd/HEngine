namespace HEngine.Foundation.Attributes;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class RangeAttribute : Attribute
{
    public float Min { get; }

    public float Max { get; }

    public RangeAttribute(float min, float max)
    {
        if (min > max)
        {
            throw new ArgumentException($"Range minimum {min} must not exceed maximum {max}.", nameof(min));
        }

        Min = min;
        Max = max;
    }
}
