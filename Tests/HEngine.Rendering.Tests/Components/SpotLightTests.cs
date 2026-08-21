using HEngine.Core.Contracts;
using HEngine.Rendering.Components;
using System.Numerics;

namespace HEngine.Rendering.Tests.Components;

public class SpotLightTests
{
    [Fact]
    public void Constructor_WithDefaults_SetsCorrectValues()
    {
        var direction = new Vector3(0f, -1f, 0f);
        var color = new Vector3(1f, 1f, 1f);
        var light = new SpotLight(direction, color);
        
        Assert.Equal(Vector3.Normalize(direction), light.Direction);
        Assert.Equal(color, light.Color);
        Assert.Equal(1f, light.Intensity);
        Assert.Equal(10f, light.Range);
        Assert.Equal(MathF.PI * 30f / 180f, light.InnerConeAngle);
        Assert.Equal(MathF.PI * 45f / 180f, light.OuterConeAngle);
    }

    [Fact]
    public void Constructor_WithCustomValues_SetsCorrectValues()
    {
        var direction = new Vector3(1f, -1f, 1f);
        var color = new Vector3(0.8f, 0.6f, 0.4f);
        var intensity = 2.5f;
        var range = 15f;
        var innerAngle = 20f;
        var outerAngle = 60f;
        var light = new SpotLight(direction, color, intensity, range, innerAngle, outerAngle);
        
        Assert.Equal(Vector3.Normalize(direction), light.Direction);
        Assert.Equal(color, light.Color);
        Assert.Equal(intensity, light.Intensity);
        Assert.Equal(range, light.Range);
        Assert.Equal(MathF.PI * innerAngle / 180f, light.InnerConeAngle);
        Assert.Equal(MathF.PI * outerAngle / 180f, light.OuterConeAngle);
    }

    [Fact]
    public void Constructor_NormalizesDirection()
    {
        var direction = new Vector3(5f, -5f, 0f);
        var color = Vector3.One;
        var light = new SpotLight(direction, color);
        
        var expectedDirection = Vector3.Normalize(direction);
        Assert.Equal(expectedDirection, light.Direction);
    }

    [Fact]
    public void Constructor_ConvertsAnglesToRadians()
    {
        var direction = Vector3.UnitY;
        var color = Vector3.One;
        var innerAngle = 45f;
        var outerAngle = 90f;
        var light = new SpotLight(direction, color, innerAngle: innerAngle, outerAngle: outerAngle);
        
        Assert.Equal(MathF.PI / 4f, light.InnerConeAngle, 5);
        Assert.Equal(MathF.PI / 2f, light.OuterConeAngle, 5);
    }

    [Fact]
    public void Constructor_WithZeroDirection_HandlesCorrectly()
    {
        var direction = Vector3.Zero;
        var color = Vector3.One;
        var light = new SpotLight(direction, color);
        
        Assert.True(float.IsNaN(light.Direction.X) || light.Direction == Vector3.Zero);
    }

    [Fact]
    public void SpotLight_ImplementsIComponent()
    {
        var light = new SpotLight(Vector3.UnitY, Vector3.One);
        Assert.IsType<IComponent>(light, exactMatch: false);
    }
}
