using System.Collections;
using System.Numerics;
using System.Runtime.CompilerServices;
using HEngine.Rendering.Enums;

namespace HEngine.Rendering.Data;

public sealed class MaterialPropertyBlock : IEnumerable<MaterialProperty>
{
    private readonly Dictionary<string, MaterialProperty> _properties = new();
    private bool _isDirty = true;

    public bool IsDirty => _isDirty;
    public int PropertyCount => _properties.Count;

    public void SetFloat(string name, float value)
    {
        SetProperty(new MaterialProperty(name, value));
    }

    public void SetInt(string name, int value)
    {
        SetProperty(new MaterialProperty(name, value));
    }

    public void SetVector2(string name, Vector2 value)
    {
        SetProperty(new MaterialProperty(name, value));
    }

    public void SetVector3(string name, Vector3 value)
    {
        SetProperty(new MaterialProperty(name, value));
    }

    public void SetVector4(string name, Vector4 value)
    {
        SetProperty(new MaterialProperty(name, value));
    }

    public void SetColor(string name, Vector4 color)
    {
        SetProperty(new MaterialProperty(name, color));
    }

    public void SetMatrix(string name, Matrix4x4 matrix)
    {
        SetProperty(new MaterialProperty(name, matrix));
    }

    public void SetTexture(string name, string texturePath)
    {
        SetProperty(new MaterialProperty(name, texturePath, MaterialPropertyType.Texture2D));
    }

    public void SetCubeTexture(string name, string texturePath)
    {
        SetProperty(new MaterialProperty(name, texturePath, MaterialPropertyType.TextureCube));
    }

    public bool TryGetProperty(string name, out MaterialProperty property)
    {
        return _properties.TryGetValue(name, out property);
    }

    public bool HasProperty(string name)
    {
        return _properties.ContainsKey(name);
    }

    public float GetFloat(string name, float defaultValue = 0f)
    {
        return TryGetProperty(name, out var prop) ? prop.AsFloat() : defaultValue;
    }

    public int GetInt(string name, int defaultValue = 0)
    {
        return TryGetProperty(name, out var prop) ? prop.AsInt() : defaultValue;
    }

    public Vector2 GetVector2(string name, Vector2 defaultValue = default)
    {
        return TryGetProperty(name, out var prop) ? prop.AsVector2() : defaultValue;
    }

    public Vector3 GetVector3(string name, Vector3 defaultValue = default)
    {
        return TryGetProperty(name, out var prop) ? prop.AsVector3() : defaultValue;
    }

    public Vector4 GetVector4(string name, Vector4 defaultValue = default)
    {
        return TryGetProperty(name, out var prop) ? prop.AsVector4() : defaultValue;
    }

    public Matrix4x4 GetMatrix(string name, Matrix4x4 defaultValue = default)
    {
        return TryGetProperty(name, out var prop) ? prop.AsMatrix4x4() : defaultValue;
    }

    public string GetTexture(string name, string defaultValue = "")
    {
        return TryGetProperty(name, out var prop) ? prop.AsTexturePath() : defaultValue;
    }

    public void RemoveProperty(string name)
    {
        if (_properties.Remove(name))
        {
            _isDirty = true;
        }
    }

    public void Clear()
    {
        _properties.Clear();
        _isDirty = true;
    }

    public void MarkClean()
    {
        _isDirty = false;
    }

    public MaterialPropertyBlock Clone()
    {
        var clone = new MaterialPropertyBlock();
        foreach (var property in _properties.Values)
        {
            clone._properties[property.Name] = property;
        }
        clone._isDirty = _isDirty;
        return clone;
    }

    public IEnumerable<MaterialProperty> GetProperties()
    {
        return _properties.Values;
    }

    public IEnumerator<MaterialProperty> GetEnumerator()
    {
        return _properties.Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetProperty(MaterialProperty property)
    {
        _properties[property.Name] = property;
        _isDirty = true;
    }
}
