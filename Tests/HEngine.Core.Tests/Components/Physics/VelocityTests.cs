using HEngine.Core.Components.Physics;
using System.Numerics;

namespace HEngine.Core.Tests.Components.Physics;

public class VelocityTests {
    [Fact]
    public void Constructor_WithNoParameters_InitializesDefaultValues()
    {
        var velocity = new Velocity();

        Assert.Equal(Vector3.Zero, velocity.Linear);
        Assert.Equal(Vector3.Zero, velocity.Angular);
    }

    [Fact]
    public void Constructor_WithLinearOnly_InitializesCorrectly()
    {
        var linear = new Vector3(1f, 2f, 3f);

        var velocity = new Velocity(linear);

        Assert.Equal(linear, velocity.Linear);
        Assert.Equal(Vector3.Zero, velocity.Angular);
    }

    [Fact]
    public void Constructor_WithBothParameters_InitializesCorrectly()
    {
        var linear = new Vector3(1f, 2f, 3f);
        var angular = new Vector3(4f, 5f, 6f);

        var velocity = new Velocity(linear, angular);

        Assert.Equal(linear, velocity.Linear);
        Assert.Equal(angular, velocity.Angular);
    }

    [Fact]
    public void Speed_ReturnsCorrectValue()
    {
        var velocity = new Velocity(new Vector3(3f, 4f, 0f));

        Assert.Equal(5f, velocity.Speed);
    }

    [Fact]
    public void AngularSpeed_ReturnsCorrectValue()
    {
        var velocity = new Velocity(Vector3.Zero, new Vector3(0f, 3f, 4f));

        Assert.Equal(5f, velocity.AngularSpeed);
    }

    [Fact]
    public void Speed_WhenZero_ReturnsZero()
    {
        var velocity = new Velocity();

        Assert.Equal(0f, velocity.Speed);
    }

    [Fact]
    public void AngularSpeed_WhenZero_ReturnsZero()
    {
        var velocity = new Velocity();

        Assert.Equal(0f, velocity.AngularSpeed);
    }

    [Fact]
    public void Properties_CanBeModified()
    {
        var velocity = new Velocity();
        var newLinear = new Vector3(10f, 20f, 30f);
        var newAngular = new Vector3(40f, 50f, 60f);

        velocity.Linear = newLinear;
        velocity.Angular = newAngular;

        Assert.Equal(newLinear, velocity.Linear);
        Assert.Equal(newAngular, velocity.Angular);
    }
}