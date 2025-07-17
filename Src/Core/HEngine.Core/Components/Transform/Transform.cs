using HEngine.Core.Contracts;
using System.Numerics;

namespace HEngine.Core.Components.Transform;

public struct Transform : IComponent {
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Scale;

    public Transform()
    {
        Position = Vector3.Zero;
        Rotation = Quaternion.Identity;
        Scale = Vector3.One;
    }

    public Transform(Vector3 position, Quaternion rotation = default, Vector3 scale = default)
    {
        Position = position;
        Rotation = rotation == default ?
            Quaternion.Identity :
            rotation;
        if (scale == default)
            Scale = Vector3.One;
        else if (scale is { X: 0 } or { Y: 0 } or { Z: 0 })
            throw new ArgumentException("Scale nie może zawierać wartości zero");
        else
            Scale = scale;
    }

    /// <summary>
    ///     Creates transformation matrix in TRS order (Translation * Rotation * Scale)
    ///     This follows industry standard used by Unity, Unreal Engine, etc.
    /// </summary>
    public Matrix4x4 ToMatrix()
        => Matrix4x4.CreateScale(Scale) *
            Matrix4x4.CreateFromQuaternion(Rotation) *
            Matrix4x4.CreateTranslation(Position);

    public Vector3 TransformPoint(Vector3 point)
        => Vector3.Transform(point, ToMatrix());

    public Vector3 TransformDirection(Vector3 direction)
        => Vector3.Transform(direction, Rotation);
}