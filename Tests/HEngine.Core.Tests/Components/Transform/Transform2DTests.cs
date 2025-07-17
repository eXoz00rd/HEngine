using HEngine.Core.Components.Transform;
using System.Numerics;

namespace HEngine.Core.Tests.Components.Transform;

public class Transform2DTests {
    [Fact]
    public void Constructor_WithDefaultValues_SetsCorrectDefaults()
    {
        var transform = new Transform2D();

        Assert.Equal(Vector2.Zero, transform.Position);
        Assert.Equal(0f, transform.Rotation);
        Assert.Equal(Vector2.One, transform.Scale); // Scale powinno być (1,1), nie (0,0)
    }
    
    [Fact]
    public void Constructor_WithAllParameters_SetsCorrectValues()
    {
        var position = new Vector2(1, 2);
        var rotation = MathF.PI / 4;
        var scale = new Vector2(2, 3);

        var transform = new Transform2D(position, rotation, scale);

        Assert.Equal(position, transform.Position);
        Assert.Equal(rotation, transform.Rotation);
        Assert.Equal(scale, transform.Scale);
    }

    [Fact]
    public void Constructor_WithDefaultScale_SetsOne()
    {
        var transform = new Transform2D(Vector2.Zero);

        Assert.Equal(Vector2.One, transform.Scale);
    }

    [Fact]
    public void Constructor_WithZeroScaleX_ThrowsArgumentException()
    {
        var scale = new Vector2(0, 1);

        var exception = Assert.Throws<ArgumentException>(() => new Transform2D(Vector2.Zero, 0f, scale));
        Assert.Equal("Scale nie może zawierać wartości zero", exception.Message);
    }

    [Fact]
    public void Constructor_WithZeroScaleY_ThrowsArgumentException()
    {
        var scale = new Vector2(1, 0);

        var exception = Assert.Throws<ArgumentException>(() => new Transform2D(Vector2.Zero, 0f, scale));
        Assert.Equal("Scale nie może zawierać wartości zero", exception.Message);
    }

    [Fact]
    public void Constructor_WithBothZeroScale_SetsToOne()
    {
        var scale = new Vector2(0, 0);

        var transform = new Transform2D(Vector2.Zero, 0f, scale);

        Assert.Equal(Vector2.One, transform.Scale);
    }

    [Fact]
    public void Constructor_WithValidScale_DoesNotThrow()
    {
        var scale = new Vector2(2, 3);

        var transform = new Transform2D(Vector2.Zero, 0f, scale);

        Assert.Equal(scale, transform.Scale);
    }

    [Fact]
    public void ToMatrix_WithIdentityTransform_ReturnsIdentityMatrix()
    {
        var transform = new Transform2D();
        var matrix = transform.ToMatrix();

        Assert.Equal(Matrix3x2.Identity, matrix);
    }

    [Fact]
    public void ToMatrix_WithPositionOnly_ReturnsTranslationMatrix()
    {
        var position = new Vector2(1, 2);
        var transform = new Transform2D(position);
        var matrix = transform.ToMatrix();
        var expected = Matrix3x2.CreateTranslation(position);

        Assert.Equal(expected, matrix);
    }

    [Fact]
    public void ToMatrix_WithRotationOnly_ReturnsRotationMatrix()
    {
        var rotation = MathF.PI / 4;
        var transform = new Transform2D(Vector2.Zero, rotation);
        var matrix = transform.ToMatrix();
        var expected = Matrix3x2.CreateRotation(rotation);

        Assert.Equal(expected, matrix);
    }

    [Fact]
    public void ToMatrix_WithScaleOnly_ReturnsScaleMatrix()
    {
        var scale = new Vector2(2, 3);
        var transform = new Transform2D(Vector2.Zero, 0f, scale);
        var matrix = transform.ToMatrix();
        var expected = Matrix3x2.CreateScale(scale);

        Assert.Equal(expected, matrix);
    }

    [Fact]
    public void ToMatrix_WithAllTransforms_ReturnsCorrectMatrix()
    {
        var position = new Vector2(1, 2);
        var rotation = MathF.PI / 4;
        var scale = new Vector2(2, 3);
        var transform = new Transform2D(position, rotation, scale);

        var matrix = transform.ToMatrix();
        var expected = Matrix3x2.CreateScale(scale) *
            Matrix3x2.CreateRotation(rotation) *
            Matrix3x2.CreateTranslation(position);

        Assert.Equal(expected, matrix);
    }

    [Fact]
    public void ToMatrix_CreatesCorrectMatrix()
    {
        var position = new Vector2(10, 20);
        var rotation = MathF.PI / 2;
        var scale = new Vector2(2, 3);
        var transform = new Transform2D(position, rotation, scale);

        var matrix = transform.ToMatrix();
        var expected = Matrix3x2.CreateScale(scale) *
            Matrix3x2.CreateRotation(rotation) *
            Matrix3x2.CreateTranslation(position);

        Assert.Equal(expected, matrix);
    }

    [Fact]
    public void ToMatrix_TransformsPointCorrectly()
    {
        var position = new Vector2(5, 10);
        var rotation = 0f;
        var scale = new Vector2(2, 3);
        var transform = new Transform2D(position, rotation, scale);

        var matrix = transform.ToMatrix();
        var point = new Vector2(1, 1);
        var transformedPoint = Vector2.Transform(point, matrix);

        // Punkt (1,1) skalowany (2,3) + translacja (5,10) = (7,13)
        var expected = new Vector2(7, 13);

        Assert.Equal(expected, transformedPoint);
    }
}