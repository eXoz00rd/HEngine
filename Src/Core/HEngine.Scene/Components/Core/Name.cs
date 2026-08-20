using HEngine.Core.Contracts;

namespace HEngine.Core.Components.Core;

public struct Name : IComponent {
    public string Value;

    public Name(string name)
    {
        Value = string.IsNullOrWhiteSpace(name) ?
            string.Empty :
            name;
    }

    public bool IsValid => !string.IsNullOrWhiteSpace(Value);

    public static implicit operator string(Name name)
        => name.Value;

    public static implicit operator Name(string name)
        => new(name);

    public override string ToString()
        => Value ?? string.Empty;
}