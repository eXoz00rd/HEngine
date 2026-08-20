namespace HEngine.Foundation.Attributes;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class TooltipAttribute : Attribute
{
    public string Text { get; }

    public TooltipAttribute(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        Text = text;
    }
}
