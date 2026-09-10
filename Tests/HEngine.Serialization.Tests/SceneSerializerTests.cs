using System.Text.Json.Nodes;
using HEngine.Serialization.Documents;
using HEngine.Serialization.Tests.Fixtures;

namespace HEngine.Serialization.Tests;

public class SceneSerializerTests
{
    private static SceneSerializer CreateSceneSerializer()
    {
        var registry = new ComponentSerializerRegistry([]);
        registry.Register(new TestPositionSerializer());
        return new SceneSerializer(registry);
    }

    [Fact]
    public void RoundTrip_ThroughJsonText_PreservesComponentValues()
    {
        var sceneSerializer = CreateSceneSerializer();
        var original = new TestPosition { X = 1.5f, Y = -2.25f };

        var componentDocument = sceneSerializer.WriteComponent("hengine.test.position", original);
        var scene = new SceneDocument
        {
            Entities = { new EntityDocument { Components = { componentDocument } } },
        };

        var json = sceneSerializer.Serialize(scene);
        var parsedScene = sceneSerializer.Deserialize(json);
        var roundTripped = (TestPosition)sceneSerializer.ReadComponent(parsedScene.Entities[0].Components[0]);

        Assert.Equal(original.X, roundTripped.X);
        Assert.Equal(original.Y, roundTripped.Y);
    }

    [Fact]
    public void Serialize_EmptyScene_ProducesEmptyJsonArray()
    {
        var sceneSerializer = CreateSceneSerializer();

        var json = sceneSerializer.Serialize(new SceneDocument());

        Assert.Equal("[]", json);
    }

    [Fact]
    public void Deserialize_JsonThatIsNotAnArray_Throws()
    {
        var sceneSerializer = CreateSceneSerializer();

        Assert.Throws<FormatException>(() => sceneSerializer.Deserialize("{}"));
    }

    [Fact]
    public void Deserialize_ComponentEntryMissingTypeField_Throws()
    {
        var sceneSerializer = CreateSceneSerializer();
        const string json = """[{"components":[{"data":{"x":1,"y":2}}]}]""";

        Assert.Throws<FormatException>(() => sceneSerializer.Deserialize(json));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Deserialize_ComponentEntryHasEmptyTypeField_Throws(string typeId)
    {
        var sceneSerializer = CreateSceneSerializer();
        var json = $"[{{\"components\":[{{\"type\":\"{typeId}\",\"data\":{{\"x\":1,\"y\":2}}}}]}}]";

        Assert.Throws<FormatException>(() => sceneSerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_ComponentsFieldIsNotAnArray_Throws()
    {
        var sceneSerializer = CreateSceneSerializer();
        const string json = """[{"components":{}}]""";

        Assert.Throws<FormatException>(() => sceneSerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_EntityWithoutComponentsField_ProducesEntityWithNoComponents()
    {
        var sceneSerializer = CreateSceneSerializer();

        var scene = sceneSerializer.Deserialize("[{}]");

        Assert.Empty(scene.Entities[0].Components);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void WriteComponent_NullOrWhitespaceTypeId_Throws(string? typeId)
    {
        var sceneSerializer = CreateSceneSerializer();

        Assert.ThrowsAny<ArgumentException>(() => sceneSerializer.WriteComponent(typeId!, new TestPosition()));
    }

    [Fact]
    public void WriteComponent_NullComponent_Throws()
    {
        var sceneSerializer = CreateSceneSerializer();

        Assert.Throws<ArgumentNullException>(() => sceneSerializer.WriteComponent("hengine.test.position", null!));
    }

    [Fact]
    public void ReadComponent_UnregisteredTypeId_Throws()
    {
        var sceneSerializer = CreateSceneSerializer();
        var document = new ComponentDocument { TypeId = "hengine.unknown", Data = JsonValue.Create(0)! };

        Assert.Throws<InvalidOperationException>(() => sceneSerializer.ReadComponent(document));
    }
}
