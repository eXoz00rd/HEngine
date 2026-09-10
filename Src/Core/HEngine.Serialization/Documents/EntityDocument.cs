namespace HEngine.Serialization.Documents;

public sealed class EntityDocument
{
    public List<ComponentDocument> Components { get; init; } = [];
}
