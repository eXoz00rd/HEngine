namespace HEngine.Foundation.Attributes;

[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class ComponentIdAttribute : Attribute
{
    private const string RequiredPrefix = "hengine.";

    public string Id { get; }

    public ComponentIdAttribute(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        if (id.Any(char.IsWhiteSpace))
        {
            throw new ArgumentException($"Component id '{id}' must not contain whitespace.", nameof(id));
        }

        if (!id.StartsWith(RequiredPrefix, StringComparison.Ordinal) || id.Length == RequiredPrefix.Length)
        {
            throw new ArgumentException($"Component id '{id}' must start with the '{RequiredPrefix}' prefix and have a name following it.", nameof(id));
        }

        Id = id;
    }
}
