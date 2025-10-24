using HEngine.Core.Contracts;

namespace HEngine.Core.Components.Core;

public struct Active : IComponent {
    public bool IsActive;

    public Active(bool isActive = true)
    {
        IsActive = isActive;
    }

    public bool IsValid => true;

    public static implicit operator bool(Active active)
        => active.IsActive;

    public static implicit operator Active(bool isActive)
        => new(isActive);
}