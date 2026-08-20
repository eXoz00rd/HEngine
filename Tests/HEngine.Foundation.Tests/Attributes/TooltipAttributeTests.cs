using HEngine.Foundation.Attributes;

namespace HEngine.Foundation.Tests.Attributes;

public class TooltipAttributeTests
{
    [Fact]
    public void Constructor_WithValidText_ShouldStoreText()
    {
        var attribute = new TooltipAttribute("World-space position of the entity.");

        Assert.Equal("World-space position of the entity.", attribute.Text);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithNullOrWhitespaceText_ShouldThrow(string? text)
    {
        Assert.ThrowsAny<ArgumentException>(() => new TooltipAttribute(text!));
    }
}
