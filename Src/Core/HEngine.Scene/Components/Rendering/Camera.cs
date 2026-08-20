using HEngine.Core.Contracts;
using System.Numerics;

namespace HEngine.Core.Components.Rendering;

public struct Camera : IComponent {
    public float FieldOfView;
    public float NearPlane;
    public float FarPlane;
    public float AspectRatio;
    public bool IsOrthographic;
    public float OrthographicSize;

    public Vector3 Position;
    public Vector3 Target;
    public Vector3 Up; 
    
    public CameraClearFlags ClearFlags;
    public Color BackgroundColor;
    public int CullingMask;
    public float Depth;

    public Camera(float fov = MathF.PI / 4f, float near = 0.1f, float far = 1000f, float aspect = 16f / 9f)
    {
        FieldOfView = fov;
        NearPlane = near;
        FarPlane = far;
        AspectRatio = aspect;
        IsOrthographic = false;
        OrthographicSize = 5f;
        
        Position = new Vector3(0, 0, 5);
        Target = Vector3.Zero;
        Up = Vector3.UnitY;
        
        ClearFlags = CameraClearFlags.SolidColor;
        BackgroundColor = Color.Black;
        CullingMask = -1;
        Depth = 0f;
    }

    public Matrix4x4 GetViewMatrix()
        => Matrix4x4.CreateLookAt(Position, Target, Up);

    public Matrix4x4 GetProjectionMatrix()
        => IsOrthographic ?
            Matrix4x4.CreateOrthographic(OrthographicSize * AspectRatio, OrthographicSize, NearPlane, FarPlane) :
            Matrix4x4.CreatePerspectiveFieldOfView(FieldOfView, AspectRatio, NearPlane, FarPlane);
}

public enum CameraClearFlags {
    SolidColor,
    Skybox,
    DepthOnly,
    Nothing
}