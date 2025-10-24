using System.Numerics;

namespace HEngine.Core.Primitives;

public readonly struct Aabb
{
    public Vector3 Min { get; }
    public Vector3 Max { get; }

    public Vector3 Center => (Min + Max) * 0.5f;
    public Vector3 Extents => (Max - Min) * 0.5f;

    public Aabb(Vector3 min, Vector3 max)
    {
        Min = new Vector3(
            float.Min(min.X, max.X),
            float.Min(min.Y, max.Y),
            float.Min(min.Z, max.Z));
        Max = new Vector3(
            float.Max(min.X, max.X),
            float.Max(min.Y, max.Y),
            float.Max(min.Z, max.Z));
    }

    public static Aabb FromCenterExtents(Vector3 center, Vector3 extents)
    {
        var min = center - extents;
        var max = center + extents;
        return new Aabb(min, max);
    }
}
