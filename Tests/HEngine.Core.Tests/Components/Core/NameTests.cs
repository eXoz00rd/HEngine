using HEngine.Core.Components.Core;

namespace HEngine.Core.Tests.Components.Core;

public class NameTests {
    [Fact]
    public void Constructor_WithValidName_ShouldSetValue()
    {
        
        const string expectedName = "TestEntity";

        
        var name = new Name(expectedName);

        
        Assert.Equal(expectedName, name.Value);
        Assert.True(name.IsValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidName_ShouldSetEmptyString(string? invalidName)
    {
        
        var name = new Name(invalidName!);

        
        Assert.Equal(string.Empty, name.Value);
        Assert.False(name.IsValid);
    }

    [Fact]
    public void ImplicitConversion_ToString_ShouldReturnValue()
    {
        
        const string expectedName = "TestEntity";
        var name = new Name(expectedName);

        
        string result = name;

        
        Assert.Equal(expectedName, result);
    }

    [Fact]
    public void ImplicitConversion_FromString_ShouldCreateCorrectName()
    {
        
        const string expectedName = "TestEntity";

        
        Name name = expectedName;

        
        Assert.Equal(expectedName, name.Value);
    }

    [Theory]
    [InlineData("ValidName", true)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValid_ShouldReturnCorrectValue(string? nameValue, bool expectedValid)
    {
        
        var name = new Name(nameValue!);

        
        Assert.Equal(expectedValid, name.IsValid);
    }

    [Theory]
    [InlineData("TestEntity", "TestEntity")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void ToString_ShouldReturnCorrectValue(string? input, string expected)
    {
        
        var name = new Name(input!);

        
        var result = name.ToString();

        
        Assert.Equal(expected, result);
    }
}