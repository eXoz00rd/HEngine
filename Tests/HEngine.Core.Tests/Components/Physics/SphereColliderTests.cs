using HEngine.Core.Components.Physics;
using System.Numerics;

namespace HEngine.Core.Tests.Components.Physics;

public class SphereColliderTests {
    [Fact]
    public void Constructor_WithRadiusOnly_InitializesCorrectly()
    {
        var radius = 2.5f;

        var sphereCollider = new SphereCollider(radius);

        Assert.Equal(radius, sphereCollider.Radius);
        Assert.Equal(Vector3.Zero, sphereCollider.Center);
    }

    [Fact]
    public void Constructor_WithBothParameters_InitializesCorrectly()
    {
        var radius = 3f;
        var center = new Vector3(1f, 2f, 3f);

        var sphereCollider = new SphereCollider(radius, center);

        Assert.Equal(radius, sphereCollider.Radius);
        Assert.Equal(center, sphereCollider.Center);
    }

    [Fact]
    public void Properties_CanBeModified()
    {
        var sphereCollider = new SphereCollider();
        var newRadius = 4f;
        var newCenter = new Vector3(5f, 6f, 7f);

        sphereCollider.Radius = newRadius;
        sphereCollider.Center = newCenter;

        Assert.Equal(newRadius, sphereCollider.Radius);
        Assert.Equal(newCenter, sphereCollider.Center);
    }
}