using HEngine.Foundation.Attributes;

namespace HEngine.Foundation.Tests.Attributes;

public class RangeAttributeTests
{
    [Fact]
    public void Constructor_WithMinLessThanMax_ShouldStoreBounds()
    {
        var attribute = new RangeAttribute(0f, 1f);

        Assert.Equal(0f, attribute.Min);
        Assert.Equal(1f, attribute.Max);
    }

    [Fact]
    public void Constructor_WithEqualMinAndMax_ShouldStoreBounds()
    {
        var attribute = new RangeAttribute(5f, 5f);

        Assert.Equal(5f, attribute.Min);
        Assert.Equal(5f, attribute.Max);
    }

    [Fact]
    public void Constructor_WithMinGreaterThanMax_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() => new RangeAttribute(1f, 0f));
    }
}
