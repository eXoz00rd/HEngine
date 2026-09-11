using HEngine.Core.Contracts;
using System.Numerics;

namespace HEngine.Core.Components.Transform;

public struct Transform2D : IComponent {
    public Vector2 Position;
    public float Rotation;
    public Vector2 Scale;

    public Transform2D()
    {
        Position = Vector2.Zero;
        Rotation = 0f;
        Scale = Vector2.One;
    }

    public Transform2D(Vector2 position, float rotation = 0f, Vector2 scale = default)
    {
        Position = position;
        Rotation = rotation;

        if (scale == default)
            Scale = Vector2.One;
        else if (scale is { X: 0 } or { Y: 0 })
            throw new ArgumentException("Scale nie może zawierać wartości zero");
        else
            Scale = scale;
    }

    public Matrix3x2 ToMatrix()
        => Matrix3x2.CreateScale(Scale) *
            Matrix3x2.CreateRotation(Rotation) *
            Matrix3x2.CreateTranslation(Position);
}