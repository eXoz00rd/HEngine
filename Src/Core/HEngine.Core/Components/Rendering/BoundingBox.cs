using HEngine.Core.Contracts;
using System.Numerics;

namespace HEngine.Core.Components.Rendering;

public struct BoundingBox : IComponent
{
    public Vector3 Center;  
    public Vector3 Extents;  

    public BoundingBox(in Vector3 center, in Vector3 extents)
    {
        Center = center;
        Extents = new Vector3(System.MathF.Abs(extents.X), System.MathF.Abs(extents.Y), System.MathF.Abs(extents.Z));
    }

    public static BoundingBox FromMinMax(in Vector3 min, in Vector3 max)
    {
        var center = (min + max) * 0.5f;
        var extents = (max - min) * 0.5f;
        return new BoundingBox(center, extents);
    }
}
