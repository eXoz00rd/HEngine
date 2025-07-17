using HEngine.Core.Components.Rendering;
using HEngine.Core.Contracts;
using System.Numerics;

namespace HEngine.Core.Tests.Components.Rendering;

public class PointLightTests
{
    [Fact]
    public void Constructor_WithDefaults_SetsCorrectValues()
    {
        var color = new Vector3(1f, 0.5f, 0.2f);
        var light = new PointLight(color);
        
        Assert.Equal(color, light.Color);
        Assert.Equal(1f, light.Intensity);
        Assert.Equal(10f, light.Range);
        Assert.Equal(1f, light.Attenuation);
    }

    [Fact]
    public void Constructor_WithCustomValues_SetsCorrectValues()
    {
        var color = new Vector3(0.8f, 0.6f, 0.4f);
        var intensity = 2.5f;
        var range = 15f;
        var attenuation = 0.8f;
        var light = new PointLight(color, intensity, range, attenuation);
        
        Assert.Equal(color, light.Color);
        Assert.Equal(intensity, light.Intensity);
        Assert.Equal(range, light.Range);
        Assert.Equal(attenuation, light.Attenuation);
    }

    [Fact]
    public void Constructor_ClampsNegativeIntensity()
    {
        var color = Vector3.One;
        var light = new PointLight(color, intensity: -5f);
        
        Assert.Equal(0f, light.Intensity);
    }

    [Fact]
    public void Constructor_ClampsNegativeRange()
    {
        var color = Vector3.One;
        var light = new PointLight(color, range: -10f);
        
        Assert.Equal(0f, light.Range);
    }

    [Fact]
    public void Constructor_ClampsNegativeAttenuation()
    {
        var color = Vector3.One;
        var light = new PointLight(color, attenuation: -2f);
        
        Assert.Equal(0f, light.Attenuation);
    }

    [Fact]
    public void Constructor_AllowsZeroValues()
    {
        var color = Vector3.One;
        var light = new PointLight(color, intensity: 0f, range: 0f, attenuation: 0f);
        
        Assert.Equal(0f, light.Intensity);
        Assert.Equal(0f, light.Range);
        Assert.Equal(0f, light.Attenuation);
    }

    [Fact]
    public void PointLight_ImplementsIComponent()
    {
        var light = new PointLight(Vector3.One);
        Assert.IsType<IComponent>(light, exactMatch: false);
    }
}
