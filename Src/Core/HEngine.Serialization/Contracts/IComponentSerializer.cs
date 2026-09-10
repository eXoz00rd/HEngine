using System.Text.Json.Nodes;

namespace HEngine.Serialization.Contracts;

public interface IComponentSerializer
{
    string TypeId { get; }

    JsonNode Write(object component);

    object Read(JsonNode data);
}
