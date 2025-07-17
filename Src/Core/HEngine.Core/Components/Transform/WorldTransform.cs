using HEngine.Core.Contracts;
using System.Numerics;

namespace HEngine.Core.Components.Transform;

public struct WorldTransform : IComponent {
    public Matrix4x4 Matrix;
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Scale;

    public WorldTransform(Matrix4x4 matrix)
    {
        Matrix = matrix;
        Matrix4x4.Decompose(matrix, out Scale, out Rotation, out Position);
    }

    public WorldTransform(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        if (scale is { X: 0 } or { Y: 0 } or { Z: 0 })
            throw new ArgumentException("Scale nie może zawierać wartości zero");

        Position = position;
        Rotation = rotation;
        Scale = scale;
        Matrix = Matrix4x4.CreateScale(scale) *
            Matrix4x4.CreateFromQuaternion(rotation) *
            Matrix4x4.CreateTranslation(position);
    }

    public void UpdateMatrix()
        => Matrix = Matrix4x4.CreateScale(Scale) *
            Matrix4x4.CreateFromQuaternion(Rotation) *
            Matrix4x4.CreateTranslation(Position);
}