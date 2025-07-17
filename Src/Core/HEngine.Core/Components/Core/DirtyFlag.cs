using HEngine.Core.Contracts;

namespace HEngine.Core.Components.Core;

/// <summary>
///     Tag oznaczający encję jako "dirty" - wymagającą aktualizacji
/// </summary>
public struct DirtyFlag : IComponent {
    public uint Flags;

    public DirtyFlag(uint flags = 0)
    {
        Flags = flags;
    }

    public bool IsValid => true; // DirtyFlag jest zawsze ważny
    public bool HasAnyFlags => Flags != 0;

    public bool HasFlag(uint flag)
        => (Flags & flag) != 0;

    public void SetFlag(uint flag)
        => Flags |= flag;

    public void ClearFlag(uint flag)
        => Flags &= ~flag;

    public void Clear()
        => Flags = 0;

    /// <summary>
    ///     Sprawdza czy ma wszystkie podane flagi
    /// </summary>
    public bool HasAllFlags(uint flags)
        => (Flags & flags) == flags;

    /// <summary>
    ///     Sprawdza czy ma którąkolwiek z podanych flag
    /// </summary>
    public bool HasAnyFlag(uint flags)
        => (Flags & flags) != 0;
}