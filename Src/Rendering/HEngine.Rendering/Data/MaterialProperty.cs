using System.Numerics;
using HEngine.Rendering.Enums;

namespace HEngine.Rendering.Data;

public readonly struct MaterialProperty : IEquatable<MaterialProperty>
{
    public string Name { get; init; }
    public MaterialPropertyType Type { get; init; }
    public object Value { get; init; }

    public MaterialProperty(string name, float value)
    {
        Name = name;
        Type = MaterialPropertyType.Float;
        Value = value;
    }

    public MaterialProperty(string name, int value)
    {
        Name = name;
        Type = MaterialPropertyType.Int;
        Value = value;
    }

    public MaterialProperty(string name, Vector2 value)
    {
        Name = name;
        Type = MaterialPropertyType.Vector2;
        Value = value;
    }

    public MaterialProperty(string name, Vector3 value)
    {
        Name = name;
        Type = MaterialPropertyType.Vector3;
        Value = value;
    }

    public MaterialProperty(string name, Vector4 value)
    {
        Name = name;
        Type = MaterialPropertyType.Vector4;
        Value = value;
    }

    public MaterialProperty(string name, Matrix4x4 value)
    {
        Name = name;
        Type = MaterialPropertyType.Matrix4x4;
        Value = value;
    }

    public MaterialProperty(string name, string texturePath, MaterialPropertyType textureType)
    {
        if (textureType != MaterialPropertyType.Texture2D && textureType != MaterialPropertyType.TextureCube)
            throw new ArgumentException("Invalid texture type", nameof(textureType));

        Name = name;
        Type = textureType;
        Value = texturePath;
    }

    public T GetValue<T>()
    {
        if (Value is not T typedValue)
            throw new InvalidCastException($"Cannot cast property '{Name}' of type {Type} to {typeof(T).Name}");
        return typedValue;
    }

    public float AsFloat() => GetValue<float>();
    public int AsInt() => GetValue<int>();
    public Vector2 AsVector2() => GetValue<Vector2>();
    public Vector3 AsVector3() => GetValue<Vector3>();
    public Vector4 AsVector4() => GetValue<Vector4>();
    public Matrix4x4 AsMatrix4x4() => GetValue<Matrix4x4>();
    public string AsTexturePath() => GetValue<string>();

    public bool Equals(MaterialProperty other)
    {
        return Name == other.Name && Type == other.Type && Equals(Value, other.Value);
    }

    public override bool Equals(object? obj)
    {
        return obj is MaterialProperty other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, Type, Value);
    }

    public static bool operator ==(MaterialProperty left, MaterialProperty right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(MaterialProperty left, MaterialProperty right)
    {
        return !left.Equals(right);
    }
}
