using HEngine.Serialization.Tests.Fixtures;

namespace HEngine.Serialization.Tests;

public class ComponentSerializerRegistryTests
{
    [Fact]
    public void Register_ThenGet_ReturnsTheSameSerializer()
    {
        var registry = new ComponentSerializerRegistry([]);
        var serializer = new TestPositionSerializer();

        registry.Register(serializer);

        Assert.Same(serializer, registry.Get("hengine.test.position"));
    }

    [Fact]
    public void Register_DuplicateTypeId_Throws()
    {
        var registry = new ComponentSerializerRegistry([]);
        registry.Register(new TestPositionSerializer());

        Assert.Throws<InvalidOperationException>(() => registry.Register(new TestPositionSerializer()));
    }

    [Fact]
    public void Get_UnknownTypeId_Throws()
    {
        var registry = new ComponentSerializerRegistry([]);

        Assert.Throws<InvalidOperationException>(() => registry.Get("hengine.unknown"));
    }

    [Fact]
    public void TryGet_UnknownTypeId_ReturnsFalse()
    {
        var registry = new ComponentSerializerRegistry([]);

        Assert.False(registry.TryGet("hengine.unknown", out _));
    }

    [Fact]
    public void Constructor_WithSerializers_RegistersEachOfThem()
    {
        var serializer = new TestPositionSerializer();

        var registry = new ComponentSerializerRegistry([serializer]);

        Assert.Same(serializer, registry.Get("hengine.test.position"));
    }
}
