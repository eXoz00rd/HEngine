using HEngine.Core.Contracts;
using HEngine.Rendering.Components;
using System.Numerics;

namespace HEngine.Rendering.Tests.Components;

public class DirectionalLightTests
{
    [Fact]
    public void Constructor_WithDefaults_SetsCorrectValues()
    {
        var direction = new Vector3(1f, -1f, 0f);
        var color = new Vector3(1f, 1f, 1f);
        var light = new DirectionalLight(direction, color);
        
        Assert.Equal(Vector3.Normalize(direction), light.Direction);
        Assert.Equal(color, light.Color);
        Assert.Equal(1f, light.Intensity);
    }

    [Fact]
    public void Constructor_WithCustomIntensity_SetsCorrectValues()
    {
        var direction = new Vector3(0f, -1f, 0f);
        var color = new Vector3(0.8f, 0.6f, 0.4f);
        var intensity = 2.5f;
        var light = new DirectionalLight(direction, color, intensity);
        
        Assert.Equal(Vector3.Normalize(direction), light.Direction);
        Assert.Equal(color, light.Color);
        Assert.Equal(intensity, light.Intensity);
    }

    [Fact]
    public void Constructor_NormalizesDirection()
    {
        var direction = new Vector3(5f, -5f, 0f);
        var color = Vector3.One;
        var light = new DirectionalLight(direction, color);
        
        var expectedDirection = Vector3.Normalize(direction);
        Assert.Equal(expectedDirection, light.Direction);
    }

    [Fact]
    public void Constructor_WithZeroDirection_HandlesCorrectly()
    {
        var direction = Vector3.Zero;
        var color = Vector3.One;
        var light = new DirectionalLight(direction, color);
        
        Assert.True(float.IsNaN(light.Direction.X) || light.Direction == Vector3.Zero);
    }

    [Fact]
    public void DirectionalLight_ImplementsIComponent()
    {
        var light = new DirectionalLight(Vector3.UnitY, Vector3.One);
        Assert.IsType<IComponent>(light, exactMatch: false);
    }
}
