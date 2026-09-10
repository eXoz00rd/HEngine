using System.Diagnostics.CodeAnalysis;
using HEngine.Serialization.Contracts;

namespace HEngine.Serialization;

public sealed class ComponentSerializerRegistry
{
    private readonly Dictionary<string, IComponentSerializer> _serializersByTypeId = new();
    private readonly Dictionary<Type, IComponentSerializer> _serializersByComponentType = new();

    public ComponentSerializerRegistry(IEnumerable<IComponentSerializer> serializers)
    {
        ArgumentNullException.ThrowIfNull(serializers);

        foreach (var serializer in serializers)
        {
            Register(serializer);
        }
    }

    public void Register(IComponentSerializer serializer)
    {
        ArgumentNullException.ThrowIfNull(serializer);

        var typeId = serializer.TypeId;
        if (string.IsNullOrWhiteSpace(typeId))
        {
            throw new ArgumentException(
                "A component serializer must have a non-empty TypeId.", nameof(serializer));
        }

        if (!_serializersByTypeId.TryAdd(typeId, serializer))
        {
            throw new InvalidOperationException(
                $"A component serializer is already registered for type id '{typeId}'.");
        }

        _serializersByComponentType[serializer.ComponentType] = serializer;
    }

    public bool TryGet(string typeId, [NotNullWhen(true)] out IComponentSerializer? serializer)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(typeId);

        return _serializersByTypeId.TryGetValue(typeId, out serializer);
    }

    public bool TryGet(Type componentType, [NotNullWhen(true)] out IComponentSerializer? serializer)
    {
        ArgumentNullException.ThrowIfNull(componentType);

        return _serializersByComponentType.TryGetValue(componentType, out serializer);
    }

    public IComponentSerializer Get(string typeId)
        => TryGet(typeId, out var serializer)
            ? serializer
            : throw new InvalidOperationException($"No component serializer registered for type id '{typeId}'.");

    public IComponentSerializer Get(Type componentType)
        => TryGet(componentType, out var serializer)
            ? serializer
            : throw new InvalidOperationException(
                $"No component serializer registered for component type '{componentType.FullName}'.");
}
