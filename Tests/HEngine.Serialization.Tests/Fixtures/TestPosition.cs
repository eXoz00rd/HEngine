using System.Text.Json.Nodes;
using HEngine.Foundation.Attributes;
using HEngine.Serialization.Contracts;

namespace HEngine.Serialization.Tests.Fixtures;

[ComponentId("hengine.test.position")]
public struct TestPosition
{
    public float X;
    public float Y;
}

public sealed class TestPositionSerializer : ComponentSerializer<TestPosition>
{
    protected override JsonNode WriteValue(in TestPosition component)
        => new JsonObject { ["x"] = component.X, ["y"] = component.Y };

    protected override TestPosition ReadValue(JsonNode data)
        => new TestPosition
        {
            X = data["x"]!.GetValue<float>(),
            Y = data["y"]!.GetValue<float>(),
        };
}

public struct UnannotatedComponent
{
    public int Value;
}

public sealed class UnannotatedComponentSerializer : ComponentSerializer<UnannotatedComponent>
{
    protected override JsonNode WriteValue(in UnannotatedComponent component)
        => JsonValue.Create(component.Value)!;

    protected override UnannotatedComponent ReadValue(JsonNode data)
        => new UnannotatedComponent { Value = data.GetValue<int>() };
}

public sealed class InvalidTypeIdSerializer : IComponentSerializer
{
    public string TypeId => "   ";

    public Type ComponentType => typeof(TestPosition);

    public JsonNode Write(object component) => throw new NotSupportedException();

    public object Read(JsonNode data) => throw new NotSupportedException();
}

public sealed class DuplicateComponentTypeSerializer : IComponentSerializer
{
    public string TypeId => "hengine.test.position.duplicate";

    public Type ComponentType => typeof(TestPosition);

    public JsonNode Write(object component) => throw new NotSupportedException();

    public object Read(JsonNode data) => throw new NotSupportedException();
}
