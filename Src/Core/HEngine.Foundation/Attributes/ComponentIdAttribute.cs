namespace HEngine.Foundation.Attributes;

[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class ComponentIdAttribute : Attribute
{
    private const string RequiredPrefix = "hengine.";

    public string Id { get; }

    public ComponentIdAttribute(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        if (!id.StartsWith(RequiredPrefix, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Component id '{id}' must start with the '{RequiredPrefix}' prefix.", nameof(id));
        }

        Id = id;
    }
}
