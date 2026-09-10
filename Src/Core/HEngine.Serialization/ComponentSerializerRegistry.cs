using System.Diagnostics.CodeAnalysis;
using HEngine.Serialization.Contracts;

namespace HEngine.Serialization;

public sealed class ComponentSerializerRegistry
{
    private readonly Dictionary<string, IComponentSerializer> _serializersByTypeId = new();

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

        if (!_serializersByTypeId.TryAdd(serializer.TypeId, serializer))
        {
            throw new InvalidOperationException(
                $"A component serializer is already registered for type id '{serializer.TypeId}'.");
        }
    }

    public bool TryGet(string typeId, [NotNullWhen(true)] out IComponentSerializer? serializer)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(typeId);

        return _serializersByTypeId.TryGetValue(typeId, out serializer);
    }

    public IComponentSerializer Get(string typeId)
        => TryGet(typeId, out var serializer)
            ? serializer
            : throw new InvalidOperationException($"No component serializer registered for type id '{typeId}'.");
}
