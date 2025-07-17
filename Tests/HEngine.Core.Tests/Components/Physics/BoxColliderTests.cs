using HEngine.Core.Components.Physics;
using System.Numerics;

namespace HEngine.Core.Tests.Components.Physics;

public class BoxColliderTests {
    [Fact]
    public void Constructor_WithSizeOnly_InitializesCorrectly()
    {
        var size = new Vector3(2f, 3f, 4f);

        var boxCollider = new BoxCollider(size);

        Assert.Equal(size, boxCollider.Size);
        Assert.Equal(Vector3.Zero, boxCollider.Center);
    }

    [Fact]
    public void Constructor_WithBothParameters_InitializesCorrectly()
    {
        var size = new Vector3(2f, 3f, 4f);
        var center = new Vector3(1f, 2f, 3f);

        var boxCollider = new BoxCollider(size, center);

        Assert.Equal(size, boxCollider.Size);
        Assert.Equal(center, boxCollider.Center);
    }

    [Fact]
    public void Properties_CanBeModified()
    {
        var boxCollider = new BoxCollider();
        var newSize = new Vector3(5f, 6f, 7f);
        var newCenter = new Vector3(8f, 9f, 10f);

        boxCollider.Size = newSize;
        boxCollider.Center = newCenter;

        Assert.Equal(newSize, boxCollider.Size);
        Assert.Equal(newCenter, boxCollider.Center);
    }
}