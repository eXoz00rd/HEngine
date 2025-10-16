using System.Numerics;

namespace HEngine.Core.Components.Transform;

public class WorldTransform
{
    public Matrix4x4 Matrix { get; private set; }
    public Vector3 Position { get; private set; }
    public Quaternion Rotation { get; private set; }
    public Vector3 Scale { get; private set; }

    // Precomputed matrix (used only if object is static)
    public Matrix4x4 PrecomputedMatrix { get; private set; }

    public WorldTransform(Matrix4x4 matrix)
    {
        Matrix = matrix;
        DecomposeMatrix();
        PrecomputedMatrix = Matrix; // Cache for static use
    }

    public WorldTransform(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        Position = position;
        Rotation = rotation;
        Scale = scale;
        UpdateMatrix();
        PrecomputedMatrix = Matrix; // Cache for static use
    }

    // Only update if components change
    private void DecomposeMatrix()
    {
        Position = Vector3.Zero;
        Rotation = Quaternion.Identity;
        Scale = Vector3.One;
    }

    public void UpdateMatrix()
    {
        Matrix = Matrix4x4.CreateScale(Scale) *
                 Matrix4x4.CreateFromQuaternion(Rotation) *
                 Matrix4x4.CreateTranslation(Position);
    }

    public Vector3 TransformPoint(Vector3 point)
    {
        return Vector3.Transform(point, Matrix);
    }

    public WorldTransform Clone()
    {
        return new WorldTransform(Position, Rotation, Scale);
    }
}