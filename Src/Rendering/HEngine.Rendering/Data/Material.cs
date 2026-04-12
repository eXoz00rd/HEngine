using System.Numerics;

namespace HEngine.Rendering.Data;

public class Material
{
    private readonly MaterialPropertyBlock _propertyBlock = new();

    public MaterialPropertyBlock PropertyBlock => _propertyBlock;

    public Vector4 DiffuseColor
    {
        get => _propertyBlock.GetVector4("_DiffuseColor", Vector4.One);
        set => _propertyBlock.SetColor("_DiffuseColor", value);
    }

    public Vector4 SpecularColor
    {
        get => _propertyBlock.GetVector4("_SpecularColor", new Vector4(1, 1, 1, 32));
        set => _propertyBlock.SetColor("_SpecularColor", value);
    }

    public float Metallic
    {
        get => _propertyBlock.GetFloat("_Metallic", 0.0f);
        set => _propertyBlock.SetFloat("_Metallic", value);
    }

    public float Roughness
    {
        get => _propertyBlock.GetFloat("_Roughness", 0.5f);
        set => _propertyBlock.SetFloat("_Roughness", value);
    }

    public string? DiffuseTexture
    {
        get
        {
            var tex = _propertyBlock.GetTexture("_DiffuseTexture");
            return string.IsNullOrEmpty(tex) ? null : tex;
        }
        set
        {
            if (value != null)
                _propertyBlock.SetTexture("_DiffuseTexture", value);
            else
                _propertyBlock.RemoveProperty("_DiffuseTexture");
        }
    }

    public string? NormalTexture
    {
        get
        {
            var tex = _propertyBlock.GetTexture("_NormalTexture");
            return string.IsNullOrEmpty(tex) ? null : tex;
        }
        set
        {
            if (value != null)
                _propertyBlock.SetTexture("_NormalTexture", value);
            else
                _propertyBlock.RemoveProperty("_NormalTexture");
        }
    }

    public Material()
    {
        DiffuseColor = Vector4.One;
        SpecularColor = new Vector4(1, 1, 1, 32);
        Metallic = 0.0f;
        Roughness = 0.5f;
    }

    public Material(Vector4 diffuseColor)
    {
        DiffuseColor = diffuseColor;
        SpecularColor = new Vector4(1, 1, 1, 32);
        Metallic = 0.0f;
        Roughness = 0.5f;
    }

    public void SetProperty(string name, float value) => _propertyBlock.SetFloat(name, value);
    public void SetProperty(string name, int value) => _propertyBlock.SetInt(name, value);
    public void SetProperty(string name, Vector2 value) => _propertyBlock.SetVector2(name, value);
    public void SetProperty(string name, Vector3 value) => _propertyBlock.SetVector3(name, value);
    public void SetProperty(string name, Vector4 value) => _propertyBlock.SetVector4(name, value);
    public void SetProperty(string name, Matrix4x4 value) => _propertyBlock.SetMatrix(name, value);
    public void SetTexture(string name, string texturePath) => _propertyBlock.SetTexture(name, texturePath);

    public float GetFloat(string name, float defaultValue = 0f) => _propertyBlock.GetFloat(name, defaultValue);
    public int GetInt(string name, int defaultValue = 0) => _propertyBlock.GetInt(name, defaultValue);
    public Vector2 GetVector2(string name) => _propertyBlock.GetVector2(name);
    public Vector3 GetVector3(string name) => _propertyBlock.GetVector3(name);
    public Vector4 GetVector4(string name) => _propertyBlock.GetVector4(name);
    public Matrix4x4 GetMatrix(string name) => _propertyBlock.GetMatrix(name);

    public bool HasProperty(string name) => _propertyBlock.HasProperty(name);

    public MaterialInstance CreateInstance()
    {
        return new MaterialInstance(this);
    }

    public Material Clone()
    {
        var cloned = new Material();
        var clonedBlock = _propertyBlock.Clone();

        foreach (var property in clonedBlock)
        {
            switch (property.Type)
            {
                case Enums.MaterialPropertyType.Float:
                    cloned._propertyBlock.SetFloat(property.Name, property.AsFloat());
                    break;
                case Enums.MaterialPropertyType.Int:
                    cloned._propertyBlock.SetInt(property.Name, property.AsInt());
                    break;
                case Enums.MaterialPropertyType.Vector2:
                    cloned._propertyBlock.SetVector2(property.Name, property.AsVector2());
                    break;
                case Enums.MaterialPropertyType.Vector3:
                    cloned._propertyBlock.SetVector3(property.Name, property.AsVector3());
                    break;
                case Enums.MaterialPropertyType.Vector4:
                case Enums.MaterialPropertyType.Color:
                    cloned._propertyBlock.SetVector4(property.Name, property.AsVector4());
                    break;
                case Enums.MaterialPropertyType.Matrix4x4:
                    cloned._propertyBlock.SetMatrix(property.Name, property.AsMatrix4x4());
                    break;
                case Enums.MaterialPropertyType.Texture2D:
                    cloned._propertyBlock.SetTexture(property.Name, property.AsTexturePath());
                    break;
                case Enums.MaterialPropertyType.TextureCube:
                    cloned._propertyBlock.SetCubeTexture(property.Name, property.AsTexturePath());
                    break;
            }
        }
        return cloned;
    }

    public static Material Default => new()
    {
        DiffuseColor = Vector4.One,
        SpecularColor = new Vector4(1, 1, 1, 32),
        Metallic = 0.0f,
        Roughness = 0.5f
    };
}
