using HEngine.Core.Components.Physics;
using System.Numerics;

namespace HEngine.Core.Tests.Components.Physics;

public class AccelerationTests {
    [Fact]
    public void Constructor_WithNoParameters_InitializesDefaultValues()
    {
        var acceleration = new Acceleration();

        Assert.Equal(Vector3.Zero, acceleration.Linear);
        Assert.Equal(Vector3.Zero, acceleration.Angular);
    }

    [Fact]
    public void Constructor_WithLinearOnly_InitializesCorrectly()
    {
        var linear = new Vector3(1f, 2f, 3f);

        var acceleration = new Acceleration(linear);

        Assert.Equal(linear, acceleration.Linear);
        Assert.Equal(Vector3.Zero, acceleration.Angular);
    }

    [Fact]
    public void Constructor_WithBothParameters_InitializesCorrectly()
    {
        var linear = new Vector3(1f, 2f, 3f);
        var angular = new Vector3(4f, 5f, 6f);

        var acceleration = new Acceleration(linear, angular);

        Assert.Equal(linear, acceleration.Linear);
        Assert.Equal(angular, acceleration.Angular);
    }

    [Fact]
    public void Properties_CanBeModified()
    {
        var acceleration = new Acceleration();
        var newLinear = new Vector3(10f, 20f, 30f);
        var newAngular = new Vector3(40f, 50f, 60f);

        acceleration.Linear = newLinear;
        acceleration.Angular = newAngular;

        Assert.Equal(newLinear, acceleration.Linear);
        Assert.Equal(newAngular, acceleration.Angular);
    }
}