using System.Numerics;
using HEngine.Core.Primitives;

namespace HEngine.Core.Mathematics;

public readonly struct Frustum
{
    public readonly Plane Left;
    public readonly Plane Right;
    public readonly Plane Bottom;
    public readonly Plane Top;
    public readonly Plane Near;
    public readonly Plane Far;

    private Frustum(Plane left, Plane right, Plane bottom, Plane top, Plane near, Plane far)
    {
        Left = left; Right = right; Bottom = bottom; Top = top; Near = near; Far = far;
    }

    public static Frustum FromViewProjection(in Matrix4x4 viewProjection)
    {
      var m = viewProjection;
      
        var left = new Plane(
            m.M14 + m.M11,
            m.M24 + m.M21,
            m.M34 + m.M31,
            m.M44 + m.M41);

        var right = new Plane(
            m.M14 - m.M11,
            m.M24 - m.M21,
            m.M34 - m.M31,
            m.M44 - m.M41);

        var bottom = new Plane(
            m.M14 + m.M12,
            m.M24 + m.M22,
            m.M34 + m.M32,
            m.M44 + m.M42);

        var top = new Plane(
            m.M14 - m.M12,
            m.M24 - m.M22,
            m.M34 - m.M32,
            m.M44 - m.M42);

        var near = new Plane(
            m.M14 + m.M13,
            m.M24 + m.M23,
            m.M34 + m.M33,
            m.M44 + m.M43);

        var far = new Plane(
            m.M14 - m.M13,
            m.M24 - m.M23,
            m.M34 - m.M33,
            m.M44 - m.M43);

        return new Frustum(
            NormalizePlane(left),
            NormalizePlane(right),
            NormalizePlane(bottom),
            NormalizePlane(top),
            NormalizePlane(near),
            NormalizePlane(far));
    }

    private static Plane NormalizePlane(in Plane p)
    {
        var n = new Vector3(p.Normal.X, p.Normal.Y, p.Normal.Z);
        var len = n.Length();
        if (len <= 0)
            return p;
        var inv = 1.0f / len;
        return new Plane(p.Normal * inv, p.D * inv);
    }

    public bool Intersects(in Aabb box)
    {
        return PlaneIntersects(Left, box)
            && PlaneIntersects(Right, box)
            && PlaneIntersects(Bottom, box)
            && PlaneIntersects(Top, box)
            && PlaneIntersects(Near, box)
            && PlaneIntersects(Far, box);
    }

    private static bool PlaneIntersects(in Plane plane, in Aabb box)
    {
        var n = plane.Normal;
        var x = n.X >= 0 ? box.Max.X : box.Min.X;
        var y = n.Y >= 0 ? box.Max.Y : box.Min.Y;
        var z = n.Z >= 0 ? box.Max.Z : box.Min.Z;

        var distance = n.X * x + n.Y * y + n.Z * z + plane.D;
        return distance >= 0;
    }
}
