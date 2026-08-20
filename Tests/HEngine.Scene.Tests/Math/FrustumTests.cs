using System.Numerics;
using HEngine.Core.Mathematics;
using HEngine.Core.Primitives;

namespace HEngine.Core.Tests.Math;

public class FrustumTests
{
    [Fact(DisplayName = "Frustum planes are normalized from view-projection matrix")]
    public void Planes_Are_Normalized()
    {
        var view = Matrix4x4.Identity;
        var proj = Matrix4x4.CreatePerspectiveFieldOfView(
            fieldOfView: MathF.PI / 3f,
            aspectRatio: 16f / 9f,
            nearPlaneDistance: 0.1f,
            farPlaneDistance: 1000f);

        var frustum = Frustum.FromViewProjection(view * proj);

        Assert.InRange(frustum.Near.Normal.Length(), 0.99f, 1.01f);
        Assert.InRange(frustum.Far.Normal.Length(), 0.99f, 1.01f);
        Assert.InRange(frustum.Left.Normal.Length(), 0.99f, 1.01f);
        Assert.InRange(frustum.Right.Normal.Length(), 0.99f, 1.01f);
        Assert.InRange(frustum.Top.Normal.Length(), 0.99f, 1.01f);
        Assert.InRange(frustum.Bottom.Normal.Length(), 0.99f, 1.01f);
    }

    [Fact(DisplayName = "AABB inside frustum should intersect")]
    public void Aabb_Inside_Intersects()
    {
        var view = Matrix4x4.Identity;
        var proj = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 2f, 1.0f, 0.1f, 100f);
        var frustum = Frustum.FromViewProjection(view * proj);

        var box = Aabb.FromCenterExtents(new Vector3(0, 0, -5), new Vector3(0.5f));
        Assert.True(frustum.Intersects(box));
    }

    [Fact(DisplayName = "AABB far outside frustum should not intersect")]
    public void Aabb_Outside_DoesNotIntersect()
    {
        var view = Matrix4x4.Identity;
        var proj = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 2f, 1.0f, 0.1f, 100f);
        var frustum = Frustum.FromViewProjection(view * proj);

        var box = Aabb.FromCenterExtents(new Vector3(1000, 0, -5), new Vector3(0.5f));
        Assert.False(frustum.Intersects(box));
    }

    [Fact(DisplayName = "AABB before near plane should not intersect")]
    public void Aabb_Before_NearPlane_DoesNotIntersect()
    {
        var view = Matrix4x4.Identity;
        var proj = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 2f, 1.0f, 0.5f, 100f);
        var frustum = Frustum.FromViewProjection(view * proj);
        
        var box = Aabb.FromCenterExtents(new Vector3(0, 0, -0.1f), new Vector3(0.05f));
        Assert.False(frustum.Intersects(box));
    }
}
