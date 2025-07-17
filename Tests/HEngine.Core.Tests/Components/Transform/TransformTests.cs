using System.Numerics;
using Xunit.Abstractions;

namespace HEngine.Core.Tests.Components.Transform;

public class TransformTests {

    private readonly ITestOutputHelper _output;

    public TransformTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Constructor_WithDefaultValues_SetsCorrectDefaults()
    {
        var transform = new HEngine.Core.Components.Transform.Transform();

        Assert.Equal(Vector3.Zero, transform.Position);
        Assert.Equal(Quaternion.Identity, transform.Rotation);
        Assert.Equal(Vector3.One, transform.Scale);
    }

    [Fact]
    public void Constructor_WithAllParameters_SetsCorrectValues()
    {
        var position = new Vector3(1, 2, 3);
        var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 4);
        var scale = new Vector3(2, 3, 4);

        var transform = new HEngine.Core.Components.Transform.Transform(position, rotation, scale);

        Assert.Equal(position, transform.Position);
        Assert.Equal(rotation, transform.Rotation);
        Assert.Equal(scale, transform.Scale);
    }

    [Fact]
    public void Constructor_WithDefaultRotation_SetsIdentity()
    {
        var transform = new HEngine.Core.Components.Transform.Transform(Vector3.Zero, default, Vector3.One);

        Assert.Equal(Quaternion.Identity, transform.Rotation);
    }

    [Fact]
    public void Constructor_WithDefaultScale_SetsOne()
    {
        var transform = new HEngine.Core.Components.Transform.Transform(Vector3.Zero, Quaternion.Identity);

        Assert.Equal(Vector3.One, transform.Scale);
    }

    [Fact]
    public void ToMatrix_WithIdentityTransform_ReturnsIdentityMatrix()
    {
        var transform = new HEngine.Core.Components.Transform.Transform();
        var matrix = transform.ToMatrix();

        Assert.Equal(Matrix4x4.Identity, matrix);
    }

    [Fact]
    public void ToMatrix_WithPositionOnly_ReturnsTranslationMatrix()
    {
        var position = new Vector3(1, 2, 3);
        var transform = new HEngine.Core.Components.Transform.Transform(position);
        var matrix = transform.ToMatrix();
        var expected = Matrix4x4.CreateTranslation(position);

        Assert.Equal(expected, matrix);
    }

    [Fact]
    public void ToMatrix_WithRotationOnly_ReturnsRotationMatrix()
    {
        var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 4);
        var transform = new HEngine.Core.Components.Transform.Transform(Vector3.Zero, rotation);
        var matrix = transform.ToMatrix();
        var expected = Matrix4x4.CreateFromQuaternion(rotation);

        Assert.Equal(expected, matrix);
    }

    [Fact]
    public void ToMatrix_WithScaleOnly_ReturnsScaleMatrix()
    {
        var scale = new Vector3(2, 3, 4);
        var transform = new HEngine.Core.Components.Transform.Transform(Vector3.Zero, Quaternion.Identity, scale);
        var matrix = transform.ToMatrix();
        var expected = Matrix4x4.CreateScale(scale);

        Assert.Equal(expected, matrix);
    }

    [Fact]
    public void ToMatrix_WithAllTransforms_ReturnsCorrectMatrix()
    {
        var position = new Vector3(1, 2, 3);
        var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 4);
        var scale = new Vector3(2, 3, 4);
        var transform = new HEngine.Core.Components.Transform.Transform(position, rotation, scale);

        var matrix = transform.ToMatrix();
        var expected = Matrix4x4.CreateScale(scale) *
            Matrix4x4.CreateFromQuaternion(rotation) *
            Matrix4x4.CreateTranslation(position);

        Assert.Equal(expected, matrix);
    }

    [Fact]
    public void TransformPoint_WithIdentityTransform_ReturnsOriginalPoint()
    {
        var transform = new HEngine.Core.Components.Transform.Transform();
        var point = new Vector3(1, 2, 3);
        var result = transform.TransformPoint(point);

        Assert.Equal(point, result);
    }

    [Fact]
    public void TransformPoint_WithTranslation_ReturnsTranslatedPoint()
    {
        var position = new Vector3(1, 2, 3);
        var transform = new HEngine.Core.Components.Transform.Transform(position);
        var point = new Vector3(1, 1, 1);
        var result = transform.TransformPoint(point);
        var expected = new Vector3(2, 3, 4);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void TransformPoint_WithScale_ReturnsScaledPoint()
    {
        var scale = new Vector3(2, 3, 4);
        var transform = new HEngine.Core.Components.Transform.Transform(Vector3.Zero, Quaternion.Identity, scale);
        var point = new Vector3(1, 1, 1);
        var result = transform.TransformPoint(point);
        var expected = new Vector3(2, 3, 4);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void TransformPoint_WithScaleAndRotation_ReturnsCorrectPoint()
    {
        var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 2);
        var scale = new Vector3(2, 2, 1);
        var transform = new HEngine.Core.Components.Transform.Transform(Vector3.Zero, rotation, scale);
        var point = new Vector3(1, 0, 0);
        var result = transform.TransformPoint(point);
    
        var expected = new Vector3(0, 2, 0);
        Assert.True(Vector3.Distance(expected, result) < 0.001f);
    }

    [Fact]
    public void TransformPoint_WithAllTransforms_ReturnsCorrectPoint()
    {
        var position = new Vector3(1, 1, 0);
        var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 2);
        var scale = new Vector3(2, 2, 1);
        var transform = new HEngine.Core.Components.Transform.Transform(position, rotation, scale);
        var point = new Vector3(1, 0, 0);
        var result = transform.TransformPoint(point);

        var expected = new Vector3(1, 3, 0);
        var distance = Vector3.Distance(expected, result);
        Assert.True(distance < 0.001f, $"Distance was {distance}, result was ({result.X}, {result.Y}, {result.Z})");
    }

    [Fact]
    public void TransformDirection_WithIdentityTransform_ReturnsOriginalDirection()
    {
        var transform = new HEngine.Core.Components.Transform.Transform();
        var direction = new Vector3(1, 0, 0);
        var result = transform.TransformDirection(direction);

        Assert.Equal(direction, result);
    }

    [Fact]
    public void TransformDirection_WithRotation_ReturnsRotatedDirection()
    {
        var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 2);
        var transform = new HEngine.Core.Components.Transform.Transform(Vector3.Zero, rotation);
        var direction = new Vector3(1, 0, 0);
        var result = transform.TransformDirection(direction);
        var expected = new Vector3(0, 1, 0);

        Assert.True(Vector3.Distance(expected, result) < 0.001f);
    }

    [Fact]
    public void TransformDirection_IgnoresPositionAndScale()
    {
        var position = new Vector3(100, 200, 300);
        var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 2);
        var scale = new Vector3(5, 10, 15);
        var transform = new HEngine.Core.Components.Transform.Transform(position, rotation, scale);

        var direction = new Vector3(1, 0, 0);
        var result = transform.TransformDirection(direction);
        var expected = Vector3.Transform(direction, rotation);

        Assert.Equal(expected, result);
    }
}