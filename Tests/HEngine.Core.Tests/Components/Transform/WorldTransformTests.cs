using HEngine.Core.Components.Transform;
using System.Numerics;

namespace HEngine.Core.Tests.Components.Transform;

public class WorldTransformTests {
    [Fact]
    public void Constructor_WithMatrix_DecomposesCorrectly()
    {
        var position = new Vector3(1, 2, 3);
        var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 4);
        var scale = new Vector3(2, 3, 4);
        // Używamy SRT jak w implementacji
        var matrix = Matrix4x4.CreateScale(scale) *
            Matrix4x4.CreateFromQuaternion(rotation) *
            Matrix4x4.CreateTranslation(position);

        var worldTransform = new WorldTransform(matrix);

        Assert.Equal(matrix, worldTransform.Matrix);
        Assert.True(Vector3.Distance(position, worldTransform.Position) < 0.001f);
        Assert.True(Quaternion.Dot(rotation, worldTransform.Rotation) > 0.99f);
        Assert.True(Vector3.Distance(scale, worldTransform.Scale) < 0.001f);
    }

    [Fact]
    public void Constructor_WithComponents_SetsCorrectValues()
    {
        var position = new Vector3(1, 2, 3);
        var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 4);
        var scale = new Vector3(2, 3, 4);

        var worldTransform = new WorldTransform(position, rotation, scale);

        Assert.Equal(position, worldTransform.Position);
        Assert.Equal(rotation, worldTransform.Rotation);
        Assert.Equal(scale, worldTransform.Scale);
    }

    [Fact]
    public void Constructor_WithComponents_CreatesCorrectMatrix()
    {
        var position = new Vector3(1, 2, 3);
        var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 4);
        var scale = new Vector3(2, 3, 4);

        var worldTransform = new WorldTransform(position, rotation, scale);
        // Używamy SRT jak w implementacji
        var expected = Matrix4x4.CreateScale(scale) *
            Matrix4x4.CreateFromQuaternion(rotation) *
            Matrix4x4.CreateTranslation(position);

        Assert.Equal(expected, worldTransform.Matrix);
    }

    [Fact]
    public void Constructor_WithZeroScaleX_ThrowsArgumentException()
    {
        var position = new Vector3(1, 2, 3);
        var rotation = Quaternion.Identity;
        var scale = new Vector3(0, 1, 1);

        var exception = Assert.Throws<ArgumentException>(() => new WorldTransform(position, rotation, scale));
        Assert.Equal("Scale nie może zawierać wartości zero", exception.Message);
    }

    [Fact]
    public void Constructor_WithZeroScaleY_ThrowsArgumentException()
    {
        var position = new Vector3(1, 2, 3);
        var rotation = Quaternion.Identity;
        var scale = new Vector3(1, 0, 1);

        var exception = Assert.Throws<ArgumentException>(() => new WorldTransform(position, rotation, scale));
        Assert.Equal("Scale nie może zawierać wartości zero", exception.Message);
    }

    [Fact]
    public void Constructor_WithZeroScaleZ_ThrowsArgumentException()
    {
        var position = new Vector3(1, 2, 3);
        var rotation = Quaternion.Identity;
        var scale = new Vector3(1, 1, 0);

        var exception = Assert.Throws<ArgumentException>(() => new WorldTransform(position, rotation, scale));
        Assert.Equal("Scale nie może zawierać wartości zero", exception.Message);
    }

    [Fact]
    public void Constructor_WithAllZeroScale_ThrowsArgumentException()
    {
        var position = new Vector3(1, 2, 3);
        var rotation = Quaternion.Identity;
        var scale = new Vector3(0, 0, 0);

        var exception = Assert.Throws<ArgumentException>(() => new WorldTransform(position, rotation, scale));
        Assert.Equal("Scale nie może zawierać wartości zero", exception.Message);
    }

    [Fact]
    public void Constructor_WithValidScale_DoesNotThrow()
    {
        var position = new Vector3(1, 2, 3);
        var rotation = Quaternion.Identity;
        var scale = new Vector3(2, 3, 4);

        var worldTransform = new WorldTransform(position, rotation, scale);

        Assert.Equal(scale, worldTransform.Scale);
    }

    [Fact]
    public void UpdateMatrix_UpdatesMatrixCorrectly()
    {
        var position = new Vector3(1, 2, 3);
        var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 4);
        var scale = new Vector3(2, 3, 4);
        var worldTransform = new WorldTransform(position, rotation, scale);
        worldTransform.UpdateMatrix();

        // Używamy SRT jak w implementacji
        var expected = Matrix4x4.CreateScale(worldTransform.Scale) *
            Matrix4x4.CreateFromQuaternion(worldTransform.Rotation) *
            Matrix4x4.CreateTranslation(worldTransform.Position);

        Assert.Equal(expected, worldTransform.Matrix);
    }

    [Fact]
    public void UpdateMatrix_WithIdentityTransform_CreatesIdentityMatrix()
    {
        var worldTransform = new WorldTransform(Vector3.Zero, Quaternion.Identity, Vector3.One);

        worldTransform.UpdateMatrix();

        Assert.Equal(Matrix4x4.Identity, worldTransform.Matrix);
    }

    [Fact]
    public void Constructor_WithIdentityMatrix_SetsCorrectValues()
    {
        var worldTransform = new WorldTransform(Matrix4x4.Identity);

        Assert.Equal(Matrix4x4.Identity, worldTransform.Matrix);
        Assert.Equal(Vector3.Zero, worldTransform.Position);
        Assert.Equal(Quaternion.Identity, worldTransform.Rotation);
        Assert.Equal(Vector3.One, worldTransform.Scale);
    }

    [Fact]
    public void MatrixOrder_IsCorrect()
    {
        var position = new Vector3(10, 20, 30);
        var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2);
        var scale = new Vector3(2, 3, 4);
        var worldTransform = new WorldTransform(position, rotation, scale);

        var point = new Vector3(1, 0, 0);
        var transformedPoint = Vector3.Transform(point, worldTransform.Matrix);

        // Prawidłowa kolejność SRT: najpierw scale, potem rotation, na końcu translation
        var scaledPoint = Vector3.Transform(point, Matrix4x4.CreateScale(scale));
        var rotatedPoint = Vector3.Transform(scaledPoint, Matrix4x4.CreateFromQuaternion(rotation));
        var finalPoint = Vector3.Transform(rotatedPoint, Matrix4x4.CreateTranslation(position));

        Assert.True(Vector3.Distance(finalPoint, transformedPoint) < 0.001f);
    }
}