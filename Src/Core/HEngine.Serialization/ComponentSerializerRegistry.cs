using HEngine.Serialization.Contracts;

namespace HEngine.Serialization;

public sealed class ComponentSerializerRegistry
{
    private readonly Dictionary<string, IComponentSerializer> _serializersByTypeId = new();

    public void Register(IComponentSerializer serializer)
    {
        ArgumentNullException.ThrowIfNull(serializer);

        if (!_serializersByTypeId.TryAdd(serializer.TypeId, serializer))
        {
            throw new InvalidOperationException(
                $"A component serializer is already registered for type id '{serializer.TypeId}'.");
        }
    }

    public bool TryGet(string typeId, out IComponentSerializer serializer)
        => _serializersByTypeId.TryGetValue(typeId, out serializer!);

    public IComponentSerializer Get(string typeId)
        => TryGet(typeId, out var serializer)
            ? serializer
            : throw new InvalidOperationException($"No component serializer registered for type id '{typeId}'.");
}
