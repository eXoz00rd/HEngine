using System.Text.Json.Nodes;

namespace HEngine.Serialization.Documents;

public sealed class ComponentDocument
{
    public required string TypeId { get; init; }

    public required JsonNode Data { get; init; }
}
