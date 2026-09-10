using System.Reflection;
using System.Text.Json.Nodes;
using HEngine.Foundation.Attributes;

namespace HEngine.Serialization.Contracts;

public abstract class ComponentSerializer<T> : IComponentSerializer where T : struct
{
    private static readonly string CachedTypeId = ResolveTypeId();

    static ComponentSerializer()
    {
    }

    public string TypeId => CachedTypeId;

    protected abstract JsonNode WriteValue(in T component);

    protected abstract T ReadValue(JsonNode data);

    JsonNode IComponentSerializer.Write(object component)
    {
        if (component is not T value)
        {
            throw new ArgumentException(
                $"Component serializer for '{CachedTypeId}' expected a value of type '{typeof(T).FullName}' but received '{component?.GetType().FullName ?? "null"}'.",
                nameof(component));
        }

        return WriteValue(value);
    }

    object IComponentSerializer.Read(JsonNode data) => ReadValue(data);

    private static string ResolveTypeId()
    {
        var attribute = typeof(T).GetCustomAttribute<ComponentIdAttribute>()
            ?? throw new InvalidOperationException(
                $"Component type '{typeof(T).FullName}' has no [ComponentId] attribute; a stable id is required to serialize it.");

        return attribute.Id;
    }
}
