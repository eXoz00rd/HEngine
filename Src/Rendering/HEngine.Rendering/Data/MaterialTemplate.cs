namespace HEngine.Rendering.Data;

public sealed class MaterialTemplate
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public MaterialPropertyBlock Properties { get; } = new();

    public MaterialTemplate()
    {
    }

    public MaterialTemplate(string name, string description)
    {
        Name = name;
        Description = description;
    }

    public Material Instantiate()
    {
        var material = new Material();

        foreach (var property in Properties)
        {
            switch (property.Type)
            {
                case Enums.MaterialPropertyType.Float:
                    material.SetProperty(property.Name, property.AsFloat());
                    break;
                case Enums.MaterialPropertyType.Int:
                    material.SetProperty(property.Name, property.AsInt());
                    break;
                case Enums.MaterialPropertyType.Vector2:
                    material.SetProperty(property.Name, property.AsVector2());
                    break;
                case Enums.MaterialPropertyType.Vector3:
                    material.SetProperty(property.Name, property.AsVector3());
                    break;
                case Enums.MaterialPropertyType.Vector4:
                case Enums.MaterialPropertyType.Color:
                    material.SetProperty(property.Name, property.AsVector4());
                    break;
                case Enums.MaterialPropertyType.Matrix4x4:
                    material.SetProperty(property.Name, property.AsMatrix4x4());
                    break;
                case Enums.MaterialPropertyType.Texture2D:
                    material.SetTexture(property.Name, property.AsTexturePath());
                    break;
                case Enums.MaterialPropertyType.TextureCube:
                    material.PropertyBlock.SetCubeTexture(property.Name, property.AsTexturePath());
                    break;
            }
        }

        return material;
    }

    public MaterialTemplate Clone()
    {
        var cloned = new MaterialTemplate(Name, Description);

        foreach (var property in Properties)
        {
            switch (property.Type)
            {
                case Enums.MaterialPropertyType.Float:
                    cloned.Properties.SetFloat(property.Name, property.AsFloat());
                    break;
                case Enums.MaterialPropertyType.Int:
                    cloned.Properties.SetInt(property.Name, property.AsInt());
                    break;
                case Enums.MaterialPropertyType.Vector2:
                    cloned.Properties.SetVector2(property.Name, property.AsVector2());
                    break;
                case Enums.MaterialPropertyType.Vector3:
                    cloned.Properties.SetVector3(property.Name, property.AsVector3());
                    break;
                case Enums.MaterialPropertyType.Vector4:
                case Enums.MaterialPropertyType.Color:
                    cloned.Properties.SetVector4(property.Name, property.AsVector4());
                    break;
                case Enums.MaterialPropertyType.Matrix4x4:
                    cloned.Properties.SetMatrix(property.Name, property.AsMatrix4x4());
                    break;
                case Enums.MaterialPropertyType.Texture2D:
                    cloned.Properties.SetTexture(property.Name, property.AsTexturePath());
                    break;
                case Enums.MaterialPropertyType.TextureCube:
                    cloned.Properties.SetCubeTexture(property.Name, property.AsTexturePath());
                    break;
            }
        }

        return cloned;
    }
}
