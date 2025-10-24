using HEngine.Core.Contracts;

namespace HEngine.Core.Components.Core;

public struct DirtyFlag : IComponent {
    public uint Flags;

    public DirtyFlag(uint flags = 0)
    {
        Flags = flags;
    }

    public bool IsValid => true;
    public bool HasAnyFlags => Flags != 0;

    public bool HasFlag(uint flag)
        => (Flags & flag) != 0;

    public void SetFlag(uint flag)
        => Flags |= flag;

    public void ClearFlag(uint flag)
        => Flags &= ~flag;

    public void Clear()
        => Flags = 0;

    public bool HasAllFlags(uint flags)
        => (Flags & flags) == flags;

    public bool HasAnyFlag(uint flags)
        => (Flags & flags) != 0;
}