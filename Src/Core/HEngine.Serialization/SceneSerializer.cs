using System.Text.Json;
using System.Text.Json.Nodes;
using HEngine.Serialization.Documents;

namespace HEngine.Serialization;

public sealed class SceneSerializer
{
    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

    private readonly ComponentSerializerRegistry _registry;

    public SceneSerializer(ComponentSerializerRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    public ComponentDocument WriteComponent(string typeId, object component)
    {
        var serializer = _registry.Get(typeId);
        return new ComponentDocument { TypeId = typeId, Data = serializer.Write(component) };
    }

    public object ReadComponent(ComponentDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var serializer = _registry.Get(document.TypeId);
        return serializer.Read(document.Data);
    }

    public string Serialize(SceneDocument scene)
    {
        ArgumentNullException.ThrowIfNull(scene);

        var entities = new JsonArray();
        foreach (var entity in scene.Entities)
        {
            var components = new JsonArray();
            foreach (var component in entity.Components)
            {
                components.Add(new JsonObject
                {
                    ["type"] = component.TypeId,
                    ["data"] = component.Data.DeepClone(),
                });
            }

            entities.Add(new JsonObject { ["components"] = components });
        }

        return entities.ToJsonString(WriteOptions);
    }

    public SceneDocument Deserialize(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        var entities = JsonNode.Parse(json) as JsonArray
            ?? throw new FormatException("A scene document must be a JSON array of entities.");

        var scene = new SceneDocument();
        foreach (var entityNode in entities)
        {
            var entityObject = entityNode as JsonObject
                ?? throw new FormatException("Each scene entity must be a JSON object.");

            var entity = new EntityDocument();

            if (entityObject["components"] is JsonArray componentsArray)
            {
                foreach (var componentNode in componentsArray)
                {
                    entity.Components.Add(ParseComponent(componentNode));
                }
            }

            scene.Entities.Add(entity);
        }

        return scene;
    }

    private static ComponentDocument ParseComponent(JsonNode? componentNode)
    {
        var componentObject = componentNode as JsonObject
            ?? throw new FormatException("Each component entry must be a JSON object.");

        var typeId = componentObject["type"]?.GetValue<string>()
            ?? throw new FormatException("A component entry is missing its 'type' field.");
        var data = componentObject["data"]
            ?? throw new FormatException($"Component entry '{typeId}' is missing its 'data' field.");

        return new ComponentDocument { TypeId = typeId, Data = data.DeepClone() };
    }
}
