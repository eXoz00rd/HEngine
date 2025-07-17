using HEngine.Core.Components.Core;

namespace HEngine.Core.Tests.Components.Core;

public class ActiveTests {
    [Fact]
    public void Constructor_WithoutParameter_ShouldBeActive()
    {
        var active = new Active();
    
        Assert.False(active.IsActive);
        Assert.True(active.IsValid);
    }

    [Fact]
    public void Constructor_WithDefaultParameter_ShouldBeActive()
    {
        var active = new Active(true);
    
        Assert.True(active.IsActive); 
        Assert.True(active.IsValid);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Constructor_WithValue_ShouldSetCorrectValue(bool isActive)
    {
        var active = new Active(isActive);

        Assert.Equal(isActive, active.IsActive);
        Assert.True(active.IsValid);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ImplicitConversion_ToBool_ShouldReturnIsActive(bool expected)
    {
        var active = new Active(expected);

        bool result = active;

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ImplicitConversion_FromBool_ShouldCreateCorrectActive(bool value)
    {
        Active active = value;

        Assert.Equal(value, active.IsActive);
    }

    [Fact]
    public void IsValid_ShouldAlwaysBeTrue()
    {
        var active1 = new Active(true);
        var active2 = new Active(false);

        Assert.True(active1.IsValid);
        Assert.True(active2.IsValid);
    }
}