using System.Numerics;
using HEngine.Core.Rendering.Contracts;

namespace HEngine.Core.Rendering;

public sealed class FreeCamera : ICamera
{
    public Vector3 Position { get; set; } = new(0, 0, 5);

    public Vector3 Target { get; set; } = Vector3.Zero;

    public Vector3 Up { get; set; } = Vector3.UnitY;

    public float FieldOfView { get; set; } = MathF.PI / 4f;

    public float NearPlane { get; set; } = 0.1f;

    public float FarPlane { get; set; } = 1000f;

    public float AspectRatio { get; set; } = 16f / 9f;

    public Matrix4x4 ViewMatrix
        => Matrix4x4.CreateLookAt(Position, Target, Up);

    public Matrix4x4 ProjectionMatrix
    {
        get
        {
            var fov = Math.Clamp(FieldOfView, 0.01f, MathF.PI - 0.01f);
            var nearP = Math.Max(0.0001f, NearPlane);
            var farP = Math.Max(nearP + 0.001f, FarPlane);
            return Matrix4x4.CreatePerspectiveFieldOfView(fov, AspectRatio, nearP, farP);
        }
    }
}