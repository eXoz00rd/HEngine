using System.Numerics;

namespace HEngine.Rendering.Data;

public sealed class MaterialInstance
{
    private readonly Material _baseMaterial;
    private readonly MaterialPropertyBlock _overrides = new();

    public Material BaseMaterial => _baseMaterial;
    public MaterialPropertyBlock Overrides => _overrides;
    public bool HasOverrides => _overrides.PropertyCount > 0;

    public MaterialInstance(Material baseMaterial)
    {
        _baseMaterial = baseMaterial ?? throw new ArgumentNullException(nameof(baseMaterial));
    }

    public Vector4 DiffuseColor
    {
        get => GetVector4("_DiffuseColor", _baseMaterial.DiffuseColor);
        set => _overrides.SetColor("_DiffuseColor", value);
    }

    public Vector4 SpecularColor
    {
        get => GetVector4("_SpecularColor", _baseMaterial.SpecularColor);
        set => _overrides.SetColor("_SpecularColor", value);
    }

    public float Metallic
    {
        get => GetFloat("_Metallic", _baseMaterial.Metallic);
        set => _overrides.SetFloat("_Metallic", value);
    }

    public float Roughness
    {
        get => GetFloat("_Roughness", _baseMaterial.Roughness);
        set => _overrides.SetFloat("_Roughness", value);
    }

    public string? DiffuseTexture
    {
        get
        {
            var tex = GetTexture("_DiffuseTexture");
            return string.IsNullOrEmpty(tex) ? _baseMaterial.DiffuseTexture : tex;
        }
        set
        {
            if (value != null)
                _overrides.SetTexture("_DiffuseTexture", value);
            else
                _overrides.RemoveProperty("_DiffuseTexture");
        }
    }

    public string? NormalTexture
    {
        get
        {
            var tex = GetTexture("_NormalTexture");
            return string.IsNullOrEmpty(tex) ? _baseMaterial.NormalTexture : tex;
        }
        set
        {
            if (value != null)
                _overrides.SetTexture("_NormalTexture", value);
            else
                _overrides.RemoveProperty("_NormalTexture");
        }
    }

    public void SetProperty(string name, float value) => _overrides.SetFloat(name, value);
    public void SetProperty(string name, int value) => _overrides.SetInt(name, value);
    public void SetProperty(string name, Vector2 value) => _overrides.SetVector2(name, value);
    public void SetProperty(string name, Vector3 value) => _overrides.SetVector3(name, value);
    public void SetProperty(string name, Vector4 value) => _overrides.SetVector4(name, value);
    public void SetProperty(string name, Matrix4x4 value) => _overrides.SetMatrix(name, value);
    public void SetTexture(string name, string texturePath) => _overrides.SetTexture(name, texturePath);

    public float GetFloat(string name, float defaultValue = 0f)
    {
        if (_overrides.TryGetProperty(name, out var prop))
            return prop.AsFloat();
        return _baseMaterial.GetFloat(name, defaultValue);
    }

    public int GetInt(string name, int defaultValue = 0)
    {
        if (_overrides.TryGetProperty(name, out var prop))
            return prop.AsInt();
        return _baseMaterial.GetInt(name, defaultValue);
    }

    public Vector2 GetVector2(string name)
    {
        if (_overrides.TryGetProperty(name, out var prop))
            return prop.AsVector2();
        return _baseMaterial.GetVector2(name);
    }

    public Vector3 GetVector3(string name)
    {
        if (_overrides.TryGetProperty(name, out var prop))
            return prop.AsVector3();
        return _baseMaterial.GetVector3(name);
    }

    public Vector4 GetVector4(string name, Vector4 defaultValue = default)
    {
        if (_overrides.TryGetProperty(name, out var prop))
            return prop.AsVector4();
        return _baseMaterial.GetVector4(name);
    }

    public Matrix4x4 GetMatrix(string name)
    {
        if (_overrides.TryGetProperty(name, out var prop))
            return prop.AsMatrix4x4();
        return _baseMaterial.GetMatrix(name);
    }

    public string GetTexture(string name)
    {
        if (_overrides.TryGetProperty(name, out var prop))
            return prop.AsTexturePath();
        return _baseMaterial.PropertyBlock.GetTexture(name);
    }

    public bool HasProperty(string name)
    {
        return _overrides.HasProperty(name) || _baseMaterial.HasProperty(name);
    }

    public void RemoveOverride(string name)
    {
        _overrides.RemoveProperty(name);
    }

    public void ClearOverrides()
    {
        _overrides.Clear();
    }

    public MaterialPropertyBlock GetCombinedProperties()
    {
        var combined = new MaterialPropertyBlock();

        foreach (var prop in _baseMaterial.PropertyBlock)
        {
            switch (prop.Type)
            {
                case Enums.MaterialPropertyType.Float:
                    combined.SetFloat(prop.Name, prop.AsFloat());
                    break;
                case Enums.MaterialPropertyType.Int:
                    combined.SetInt(prop.Name, prop.AsInt());
                    break;
                case Enums.MaterialPropertyType.Vector2:
                    combined.SetVector2(prop.Name, prop.AsVector2());
                    break;
                case Enums.MaterialPropertyType.Vector3:
                    combined.SetVector3(prop.Name, prop.AsVector3());
                    break;
                case Enums.MaterialPropertyType.Vector4:
                case Enums.MaterialPropertyType.Color:
                    combined.SetVector4(prop.Name, prop.AsVector4());
                    break;
                case Enums.MaterialPropertyType.Matrix4x4:
                    combined.SetMatrix(prop.Name, prop.AsMatrix4x4());
                    break;
                case Enums.MaterialPropertyType.Texture2D:
                    combined.SetTexture(prop.Name, prop.AsTexturePath());
                    break;
                case Enums.MaterialPropertyType.TextureCube:
                    combined.SetCubeTexture(prop.Name, prop.AsTexturePath());
                    break;
            }
        }

        foreach (var prop in _overrides)
        {
            switch (prop.Type)
            {
                case Enums.MaterialPropertyType.Float:
                    combined.SetFloat(prop.Name, prop.AsFloat());
                    break;
                case Enums.MaterialPropertyType.Int:
                    combined.SetInt(prop.Name, prop.AsInt());
                    break;
                case Enums.MaterialPropertyType.Vector2:
                    combined.SetVector2(prop.Name, prop.AsVector2());
                    break;
                case Enums.MaterialPropertyType.Vector3:
                    combined.SetVector3(prop.Name, prop.AsVector3());
                    break;
                case Enums.MaterialPropertyType.Vector4:
                case Enums.MaterialPropertyType.Color:
                    combined.SetVector4(prop.Name, prop.AsVector4());
                    break;
                case Enums.MaterialPropertyType.Matrix4x4:
                    combined.SetMatrix(prop.Name, prop.AsMatrix4x4());
                    break;
                case Enums.MaterialPropertyType.Texture2D:
                    combined.SetTexture(prop.Name, prop.AsTexturePath());
                    break;
                case Enums.MaterialPropertyType.TextureCube:
                    combined.SetCubeTexture(prop.Name, prop.AsTexturePath());
                    break;
            }
        }

        return combined;
    }

    public MaterialInstance Clone()
    {
        var cloned = new MaterialInstance(_baseMaterial);

        foreach (var prop in _overrides)
        {
            switch (prop.Type)
            {
                case Enums.MaterialPropertyType.Float:
                    cloned._overrides.SetFloat(prop.Name, prop.AsFloat());
                    break;
                case Enums.MaterialPropertyType.Int:
                    cloned._overrides.SetInt(prop.Name, prop.AsInt());
                    break;
                case Enums.MaterialPropertyType.Vector2:
                    cloned._overrides.SetVector2(prop.Name, prop.AsVector2());
                    break;
                case Enums.MaterialPropertyType.Vector3:
                    cloned._overrides.SetVector3(prop.Name, prop.AsVector3());
                    break;
                case Enums.MaterialPropertyType.Vector4:
                case Enums.MaterialPropertyType.Color:
                    cloned._overrides.SetVector4(prop.Name, prop.AsVector4());
                    break;
                case Enums.MaterialPropertyType.Matrix4x4:
                    cloned._overrides.SetMatrix(prop.Name, prop.AsMatrix4x4());
                    break;
                case Enums.MaterialPropertyType.Texture2D:
                    cloned._overrides.SetTexture(prop.Name, prop.AsTexturePath());
                    break;
                case Enums.MaterialPropertyType.TextureCube:
                    cloned._overrides.SetCubeTexture(prop.Name, prop.AsTexturePath());
                    break;
            }
        }

        return cloned;
    }
}
