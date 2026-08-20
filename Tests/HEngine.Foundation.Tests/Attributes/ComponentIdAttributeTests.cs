using HEngine.Foundation.Attributes;

namespace HEngine.Foundation.Tests.Attributes;

public class ComponentIdAttributeTests
{
    [Fact]
    public void Constructor_WithValidPrefixedId_ShouldStoreId()
    {
        var attribute = new ComponentIdAttribute("hengine.transform");

        Assert.Equal("hengine.transform", attribute.Id);
    }

    [Theory]
    [InlineData("transform")]
    [InlineData("Hengine.transform")]
    [InlineData("hengine")]
    public void Constructor_WithoutRequiredPrefix_ShouldThrow(string id)
    {
        Assert.Throws<ArgumentException>(() => new ComponentIdAttribute(id));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithNullOrWhitespaceId_ShouldThrow(string? id)
    {
        Assert.ThrowsAny<ArgumentException>(() => new ComponentIdAttribute(id!));
    }
}
