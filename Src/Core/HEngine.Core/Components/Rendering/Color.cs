using HEngine.Core.Contracts;
using System.Numerics;

namespace HEngine.Core.Components.Rendering;

public struct Color : IComponent, IEquatable<Color> {
    public Vector4 Value;

    public Color(Vector4 color)
    {
        Value = color;
    }

    public Color(float r, float g, float b, float a = 1f)
    {
        Value = new Vector4(r, g, b, a);
    }

    public float R
    {
        get => Value.X;
        set => Value.X = value;
    }

    public float G
    {
        get => Value.Y;
        set => Value.Y = value;
    }

    public float B
    {
        get => Value.Z;
        set => Value.Z = value;
    }

    public float A
    {
        get => Value.W;
        set => Value.W = value;
    }

    public static Color White => new(Vector4.One);
    public static Color Black => new(Vector4.Zero with { W = 1f });
    public static Color Red => new(1f, 0f, 0f);
    public static Color Green => new(0f, 1f, 0f);
    public static Color Blue => new(0f, 0f, 1f);
    public static Color Yellow => new(1f, 1f, 0f);
    public static Color Cyan => new(0f, 1f, 1f);
    public static Color Magenta => new(1f, 0f, 1f);
    public static Color Transparent => new(0f, 0f, 0f, 0f);

    public Color WithAlpha(float alpha)
        => new(Value.X, Value.Y, Value.Z, alpha);

    public Color Lerp(Color other, float t)
        => Vector4.Lerp(Value, other.Value, t);

    public float Luminance => 0.299f * R + 0.587f * G + 0.114f * B;

    public bool Equals(Color other)
        => Value.Equals(other.Value);

    public override bool Equals(object? obj)
        => obj is Color other && Equals(other);

    public override int GetHashCode()
        => Value.GetHashCode();

    public static bool operator ==(Color left, Color right)
        => left.Equals(right);

    public static bool operator !=(Color left, Color right)
        => !left.Equals(right);

    public static implicit operator Vector4(Color color)
        => color.Value;

    public static implicit operator Color(Vector4 color)
        => new(color);
}