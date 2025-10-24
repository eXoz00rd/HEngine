using HEngine.Core.Components.Rendering;
using HEngine.Core.Contracts;
using System.Numerics;

namespace HEngine.Core.Tests.Components.Rendering;

public class CameraTests {
    [Fact]
    public void Constructor_WithDefaults_SetsCorrectValues()
    {
        var camera = new Camera();

        Assert.Equal(0f, camera.FieldOfView);
        Assert.Equal(0f, camera.NearPlane);
        Assert.Equal(0f, camera.FarPlane);
        Assert.Equal(0f, camera.AspectRatio);
        Assert.False(camera.IsOrthographic);
        Assert.Equal(0f, camera.OrthographicSize);
        Assert.Equal(default, camera.ClearFlags);
        Assert.Equal(default, camera.BackgroundColor);
        Assert.Equal(0, camera.CullingMask);
        Assert.Equal(0f, camera.Depth);
    }

    [Fact]
    public void Constructor_WithParameters_SetsCorrectValues()
    {
        var camera = new Camera(MathF.PI / 3f, 0.5f, 500f, 1f);

        Assert.Equal(MathF.PI / 3f, camera.FieldOfView);
        Assert.Equal(0.5f, camera.NearPlane);
        Assert.Equal(500f, camera.FarPlane);
        Assert.Equal(1f, camera.AspectRatio);
        Assert.False(camera.IsOrthographic);
        Assert.Equal(5f, camera.OrthographicSize);
        Assert.Equal(CameraClearFlags.SolidColor, camera.ClearFlags);
        Assert.Equal(Color.Black, camera.BackgroundColor);
        Assert.Equal(-1, camera.CullingMask);
        Assert.Equal(0f, camera.Depth);
    }

    [Fact]
    public void Constructor_WithCustomValues_SetsCorrectValues()
    {
        var camera = new Camera(MathF.PI / 3f, 0.5f, 500f, 1f);

        Assert.Equal(MathF.PI / 3f, camera.FieldOfView);
        Assert.Equal(0.5f, camera.NearPlane);
        Assert.Equal(500f, camera.FarPlane);
        Assert.Equal(1f, camera.AspectRatio);
    }

    [Fact]
    public void GetProjectionMatrix_Perspective_ReturnsCorrectMatrix()
    {
        var camera = new Camera(MathF.PI / 4f, 0.1f, 100f, 1f);

        var matrix = camera.GetProjectionMatrix();
        var expected = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 4f, 1f, 0.1f, 100f);

        Assert.Equal(expected, matrix);
    }

    [Fact]
    public void GetProjectionMatrix_Orthographic_ReturnsCorrectMatrix()
    {
        var camera = new Camera(aspect: 2f)
        {
            IsOrthographic = true,
            OrthographicSize = 10f
        };

        var matrix = camera.GetProjectionMatrix();
        var expected = Matrix4x4.CreateOrthographic(20f, 10f, 0.1f, 1000f);

        Assert.Equal(expected, matrix);
    }

    [Fact]
    public void Camera_ImplementsIComponent()
    {
        var camera = new Camera();
        Assert.IsType<IComponent>(camera, false);
    }

    [Fact]
    public void GetViewMatrix_ReturnsCorrectLookAt()
    {
        var camera = new Camera(aspect: 1f)
        {
            Position = new Vector3(1, 2, 3),
            Target = new Vector3(4, 5, 6),
            Up = Vector3.UnitY
        };

        var expected = Matrix4x4.CreateLookAt(new Vector3(1, 2, 3), new Vector3(4, 5, 6), Vector3.UnitY);
        var actual = camera.GetViewMatrix();
        Assert.Equal(expected, actual);
    }
}
