using HEngine.Serialization.Tests.Fixtures;

namespace HEngine.Serialization.Tests.Contracts;

public class ComponentSerializerTests
{
    [Fact]
    public void TypeId_IsResolvedFromComponentIdAttribute()
    {
        var serializer = new TestPositionSerializer();

        Assert.Equal("hengine.test.position", serializer.TypeId);
    }

    [Fact]
    public void Constructor_ComponentWithoutComponentIdAttribute_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => new UnannotatedComponentSerializer());
    }
}
