using HEngine.Core.Contracts;
using HEngine.Core.Primitives;

namespace HEngine.Core.Components.Core;

public struct Parent : IComponent {
    public Entity Value;

    public Parent(Entity parent)
    {
        Value = parent;
    }

    public bool IsValid => Value != Entity.Null;

    public static implicit operator Entity(Parent parent)
        => parent.Value;

    public static implicit operator Parent(Entity entity)
        => new(entity);
}