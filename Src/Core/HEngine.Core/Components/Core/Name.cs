using HEngine.Core.Contracts;

namespace HEngine.Core.Components.Core;

/// <summary>
///     Podstawowy tag identyfikujący nazwę encji
/// </summary>
public struct Name : IComponent {
    public string Value;

    public Name(string name)
    {
        Value = string.IsNullOrWhiteSpace(name) ? // Zmiana: IsNullOrWhiteSpace zamiast IsNullOrEmpty
            string.Empty :
            name;
    }

    public bool IsValid => !string.IsNullOrWhiteSpace(Value); // Zmiana: IsNullOrWhiteSpace

    public static implicit operator string(Name name)
        => name.Value;

    public static implicit operator Name(string name)
        => new(name);

    public override string ToString()
        => Value ?? string.Empty;
}