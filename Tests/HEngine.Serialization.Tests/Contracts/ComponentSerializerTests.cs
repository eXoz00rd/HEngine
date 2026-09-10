using HEngine.Serialization.Contracts;
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
        var exception = Assert.Throws<TypeInitializationException>(() => new UnannotatedComponentSerializer());

        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    [Fact]
    public void Write_WrongComponentType_ThrowsArgumentException()
    {
        IComponentSerializer serializer = new TestPositionSerializer();

        Assert.Throws<ArgumentException>(() => serializer.Write(42));
    }

    [Fact]
    public void Write_NullComponent_ThrowsArgumentException()
    {
        IComponentSerializer serializer = new TestPositionSerializer();

        Assert.Throws<ArgumentException>(() => serializer.Write(null!));
    }
}
