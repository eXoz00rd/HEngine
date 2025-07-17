using HEngine.Core.Components.Physics;

namespace HEngine.Core.Tests.Components.Physics;

public class RigidbodyTests {
    [Fact]
    public void Constructor_WithNoParameters_InitializesDefaultValues()
    {
        var rigidbody = new Rigidbody();

        Assert.Equal(0f, rigidbody.Mass);
        Assert.Equal(0f, rigidbody.Drag);
        Assert.Equal(0f, rigidbody.AngularDrag);
        Assert.False(rigidbody.IsKinematic);
        Assert.False(rigidbody.UseGravity);
    }

    [Fact]
    public void Constructor_WithDefaultKeyword_InitializesDefaultValues()
    {
        var rigidbody = default(Rigidbody);

        Assert.Equal(0f, rigidbody.Mass);
        Assert.Equal(0f, rigidbody.Drag);
        Assert.Equal(0f, rigidbody.AngularDrag);
        Assert.False(rigidbody.IsKinematic);
        Assert.False(rigidbody.UseGravity);
    }

    [Fact]
    public void Constructor_WithDefaultParameters_InitializesCorrectly()
    {
        var rigidbody = new Rigidbody(1f);

        Assert.Equal(1f, rigidbody.Mass);
        Assert.Equal(0f, rigidbody.Drag);
        Assert.Equal(0.05f, rigidbody.AngularDrag);
        Assert.False(rigidbody.IsKinematic);
        Assert.True(rigidbody.UseGravity);
    }

    [Fact]
    public void Constructor_WithMassOnly_InitializesCorrectly()
    {
        var mass = 2.5f;

        var rigidbody = new Rigidbody(mass);

        Assert.Equal(mass, rigidbody.Mass);
        Assert.Equal(0f, rigidbody.Drag);
        Assert.Equal(0.05f, rigidbody.AngularDrag);
        Assert.False(rigidbody.IsKinematic);
        Assert.True(rigidbody.UseGravity);
    }

    [Fact]
    public void Constructor_WithAllParameters_InitializesCorrectly()
    {
        var mass = 3f;
        var drag = 0.1f;
        var angularDrag = 0.2f;

        var rigidbody = new Rigidbody(mass, drag, angularDrag);

        Assert.Equal(mass, rigidbody.Mass);
        Assert.Equal(drag, rigidbody.Drag);
        Assert.Equal(angularDrag, rigidbody.AngularDrag);
        Assert.False(rigidbody.IsKinematic);
        Assert.True(rigidbody.UseGravity);
    }

    [Fact]
    public void InverseMass_WhenNotKinematic_ReturnsCorrectValue()
    {
        var rigidbody = new Rigidbody(2f);

        Assert.Equal(0.5f, rigidbody.InverseMass);
    }

    [Fact]
    public void InverseMass_WhenKinematic_ReturnsZero()
    {
        var rigidbody = new Rigidbody(2f) { IsKinematic = true };

        Assert.Equal(0f, rigidbody.InverseMass);
    }

    [Fact]
    public void InverseMass_WhenMassIsZero_ReturnsZero()
    {
        var rigidbody = new Rigidbody(0f);

        Assert.Equal(0f, rigidbody.InverseMass);
    }

    [Fact]
    public void InverseMass_WhenMassIsNegative_ReturnsZero()
    {
        var rigidbody = new Rigidbody(-1f);

        Assert.Equal(0f, rigidbody.InverseMass);
    }

    [Fact]
    public void Properties_CanBeModified()
    {
        var rigidbody = new Rigidbody();

        rigidbody.Mass = 5f;
        rigidbody.Drag = 0.3f;
        rigidbody.AngularDrag = 0.4f;
        rigidbody.IsKinematic = true;
        rigidbody.UseGravity = false;

        Assert.Equal(5f, rigidbody.Mass);
        Assert.Equal(0.3f, rigidbody.Drag);
        Assert.Equal(0.4f, rigidbody.AngularDrag);
        Assert.True(rigidbody.IsKinematic);
        Assert.False(rigidbody.UseGravity);
    }
}