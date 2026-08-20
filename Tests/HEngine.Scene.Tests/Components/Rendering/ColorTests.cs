using HEngine.Core.Components.Rendering;
using HEngine.Core.Contracts;
using System.Numerics;

namespace HEngine.Core.Tests.Components.Rendering;

public class ColorTests
{
    [Fact]
    public void Constructor_WithVector4_SetsCorrectValue()
    {
        var vector = new Vector4(0.5f, 0.6f, 0.7f, 0.8f);
        var color = new Color(vector);
        
        Assert.Equal(vector, color.Value);
    }

    [Fact]
    public void Constructor_WithFloats_SetsCorrectValues()
    {
        var color = new Color(0.1f, 0.2f, 0.3f, 0.4f);
        
        Assert.Equal(0.1f, color.R);
        Assert.Equal(0.2f, color.G);
        Assert.Equal(0.3f, color.B);
        Assert.Equal(0.4f, color.A);
    }

    [Fact]
    public void Constructor_WithFloatsDefaultAlpha_SetsAlphaToOne()
    {
        var color = new Color(0.1f, 0.2f, 0.3f);
        
        Assert.Equal(1f, color.A);
    }

    [Fact]
    public void Properties_SetAndGet_WorkCorrectly()
    {
        var color = new Color(0f, 0f, 0f, 0f);
        
        color.R = 0.5f;
        color.G = 0.6f;
        color.B = 0.7f;
        color.A = 0.8f;
        
        Assert.Equal(0.5f, color.R);
        Assert.Equal(0.6f, color.G);
        Assert.Equal(0.7f, color.B);
        Assert.Equal(0.8f, color.A);
    }

    [Fact]
    public void StaticColors_HaveCorrectValues()
    {
        Assert.Equal(new Vector4(1f, 1f, 1f, 1f), Color.White.Value);
        Assert.Equal(new Vector4(0f, 0f, 0f, 1f), Color.Black.Value);
        Assert.Equal(new Vector4(1f, 0f, 0f, 1f), Color.Red.Value);
        Assert.Equal(new Vector4(0f, 1f, 0f, 1f), Color.Green.Value);
        Assert.Equal(new Vector4(0f, 0f, 1f, 1f), Color.Blue.Value);
        Assert.Equal(new Vector4(1f, 1f, 0f, 1f), Color.Yellow.Value);
        Assert.Equal(new Vector4(0f, 1f, 1f, 1f), Color.Cyan.Value);
        Assert.Equal(new Vector4(1f, 0f, 1f, 1f), Color.Magenta.Value);
        Assert.Equal(new Vector4(0f, 0f, 0f, 0f), Color.Transparent.Value);
    }

    [Fact]
    public void WithAlpha_ReturnsColorWithNewAlpha()
    {
        var color = new Color(0.5f, 0.6f, 0.7f, 0.8f);
        var newColor = color.WithAlpha(0.3f);
        
        Assert.Equal(0.5f, newColor.R);
        Assert.Equal(0.6f, newColor.G);
        Assert.Equal(0.7f, newColor.B);
        Assert.Equal(0.3f, newColor.A);
    }

    [Fact]
    public void Lerp_InterpolatesCorrectly()
    {
        var color1 = Color.Red;
        var color2 = Color.Blue;
        var lerped = color1.Lerp(color2, 0.5f);
        
        Assert.Equal(0.5f, lerped.R);
        Assert.Equal(0f, lerped.G);
        Assert.Equal(0.5f, lerped.B);
        Assert.Equal(1f, lerped.A);
    }

    [Fact]
    public void Luminance_CalculatesCorrectly()
    {
        var color = new Color(1f, 0.5f, 0.2f);
        var expected = 0.299f * 1f + 0.587f * 0.5f + 0.114f * 0.2f;
        
        Assert.Equal(expected, color.Luminance, 5);
    }

    [Fact]
    public void Equals_WithSameColor_ReturnsTrue()
    {
        var color1 = new Color(0.5f, 0.6f, 0.7f, 0.8f);
        var color2 = new Color(0.5f, 0.6f, 0.7f, 0.8f);
        
        Assert.True(color1.Equals(color2));
        Assert.True(color1 == color2);
    }

    [Fact]
    public void Equals_WithDifferentColor_ReturnsFalse()
    {
        var color1 = new Color(0.5f, 0.6f, 0.7f, 0.8f);
        var color2 = new Color(0.5f, 0.6f, 0.7f, 0.9f);
        
        Assert.False(color1.Equals(color2));
        Assert.True(color1 != color2);
    }

    [Fact]
    public void GetHashCode_WithSameColor_ReturnsSameHash()
    {
        var color1 = new Color(0.5f, 0.6f, 0.7f, 0.8f);
        var color2 = new Color(0.5f, 0.6f, 0.7f, 0.8f);
        
        Assert.Equal(color1.GetHashCode(), color2.GetHashCode());
    }

    [Fact]
    public void ImplicitOperators_WorkCorrectly()
    {
        var vector = new Vector4(0.5f, 0.6f, 0.7f, 0.8f);
        var color = new Color(vector);
        
        Vector4 vectorFromColor = color;
        Color colorFromVector = vector;
        
        Assert.Equal(vector, vectorFromColor);
        Assert.Equal(color, colorFromVector);
    }

    [Fact]
    public void Color_ImplementsIComponent()
    {
        var color = new Color();
        Assert.IsType<IComponent>(color, exactMatch: false);
    }
}
