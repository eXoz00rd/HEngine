using System.Reflection;
using System.Text.Json.Nodes;
using HEngine.Foundation.Attributes;

namespace HEngine.Serialization.Contracts;

public abstract class ComponentSerializer<T> : IComponentSerializer where T : struct
{
    public string TypeId { get; } = ResolveTypeId();

    protected abstract JsonNode WriteValue(in T component);

    protected abstract T ReadValue(JsonNode data);

    JsonNode IComponentSerializer.Write(object component) => WriteValue((T)component);

    object IComponentSerializer.Read(JsonNode data) => ReadValue(data);

    private static string ResolveTypeId()
    {
        var attribute = typeof(T).GetCustomAttribute<ComponentIdAttribute>()
            ?? throw new InvalidOperationException(
                $"Component type '{typeof(T).FullName}' has no [ComponentId] attribute; a stable id is required to serialize it.");

        return attribute.Id;
    }
}
