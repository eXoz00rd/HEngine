using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using HEngine.Rendering.Data;
using HEngine.Rendering.Enums;

namespace HEngine.Rendering.Serialization;

public static class MaterialTemplateSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string SerializeToJson(MaterialTemplate template)
    {
        var data = new TemplateData
        {
            Name = template.Name,
            Description = template.Description,
            Properties = template.Properties.GetProperties().Select(p => new PropertyData
            {
                Name = p.Name,
                Type = p.Type.ToString(),
                Value = SerializePropertyValue(p)
            }).ToList()
        };

        return JsonSerializer.Serialize(data, JsonOptions);
    }

    public static MaterialTemplate DeserializeFromJson(string json)
    {
        var data = JsonSerializer.Deserialize<TemplateData>(json, JsonOptions);
        if (data == null)
            throw new InvalidOperationException("Failed to deserialize template data");

        var template = new MaterialTemplate(data.Name ?? string.Empty, data.Description ?? string.Empty);

        if (data.Properties != null)
        {
            foreach (var propData in data.Properties)
            {
                if (propData.Name == null || propData.Type == null)
                    continue;

                var type = Enum.Parse<MaterialPropertyType>(propData.Type);
                DeserializeProperty(template.Properties, propData.Name, type, propData.Value);
            }
        }

        return template;
    }

    public static void SaveToFile(MaterialTemplate template, string filePath)
    {
        var json = SerializeToJson(template);
        File.WriteAllText(filePath, json);
    }

    public static MaterialTemplate LoadFromFile(string filePath)
    {
        var json = File.ReadAllText(filePath);
        return DeserializeFromJson(json);
    }

    private static object? SerializePropertyValue(MaterialProperty property)
    {
        return property.Type switch
        {
            MaterialPropertyType.Float => property.AsFloat(),
            MaterialPropertyType.Int => property.AsInt(),
            MaterialPropertyType.Vector2 => new[] { property.AsVector2().X, property.AsVector2().Y },
            MaterialPropertyType.Vector3 => new[] { property.AsVector3().X, property.AsVector3().Y, property.AsVector3().Z },
            MaterialPropertyType.Vector4 or MaterialPropertyType.Color => new[] { property.AsVector4().X, property.AsVector4().Y, property.AsVector4().Z, property.AsVector4().W },
            MaterialPropertyType.Matrix4x4 => SerializeMatrix(property.AsMatrix4x4()),
            MaterialPropertyType.Texture2D or MaterialPropertyType.TextureCube => property.AsTexturePath(),
            _ => null
        };
    }

    private static void DeserializeProperty(MaterialPropertyBlock properties, string name, MaterialPropertyType type, object? value)
    {
        if (value == null)
            return;

        switch (type)
        {
            case MaterialPropertyType.Float:
                if (value is JsonElement elem && elem.ValueKind == JsonValueKind.Number)
                    properties.SetFloat(name, elem.GetSingle());
                break;

            case MaterialPropertyType.Int:
                if (value is JsonElement elemInt && elemInt.ValueKind == JsonValueKind.Number)
                    properties.SetInt(name, elemInt.GetInt32());
                break;

            case MaterialPropertyType.Vector2:
                if (value is JsonElement elemV2)
                {
                    var arr = elemV2.EnumerateArray().Select(e => e.GetSingle()).ToArray();
                    if (arr.Length == 2)
                        properties.SetVector2(name, new Vector2(arr[0], arr[1]));
                }
                break;

            case MaterialPropertyType.Vector3:
                if (value is JsonElement elemV3)
                {
                    var arr = elemV3.EnumerateArray().Select(e => e.GetSingle()).ToArray();
                    if (arr.Length == 3)
                        properties.SetVector3(name, new Vector3(arr[0], arr[1], arr[2]));
                }
                break;

            case MaterialPropertyType.Vector4:
            case MaterialPropertyType.Color:
                if (value is JsonElement elemV4)
                {
                    var arr = elemV4.EnumerateArray().Select(e => e.GetSingle()).ToArray();
                    if (arr.Length == 4)
                        properties.SetVector4(name, new Vector4(arr[0], arr[1], arr[2], arr[3]));
                }
                break;

            case MaterialPropertyType.Matrix4x4:
                if (value is JsonElement elemMat)
                {
                    var arr = elemMat.EnumerateArray().Select(e => e.GetSingle()).ToArray();
                    if (arr.Length == 16)
                        properties.SetMatrix(name, DeserializeMatrix(arr));
                }
                break;

            case MaterialPropertyType.Texture2D:
                if (value is JsonElement elemTex && elemTex.ValueKind == JsonValueKind.String)
                    properties.SetTexture(name, elemTex.GetString() ?? string.Empty);
                break;

            case MaterialPropertyType.TextureCube:
                if (value is JsonElement elemCube && elemCube.ValueKind == JsonValueKind.String)
                    properties.SetCubeTexture(name, elemCube.GetString() ?? string.Empty);
                break;
        }
    }

    private static float[] SerializeMatrix(Matrix4x4 matrix)
    {
        return new[]
        {
            matrix.M11, matrix.M12, matrix.M13, matrix.M14,
            matrix.M21, matrix.M22, matrix.M23, matrix.M24,
            matrix.M31, matrix.M32, matrix.M33, matrix.M34,
            matrix.M41, matrix.M42, matrix.M43, matrix.M44
        };
    }

    private static Matrix4x4 DeserializeMatrix(float[] values)
    {
        return new Matrix4x4(
            values[0], values[1], values[2], values[3],
            values[4], values[5], values[6], values[7],
            values[8], values[9], values[10], values[11],
            values[12], values[13], values[14], values[15]
        );
    }

    private class TemplateData
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public List<PropertyData>? Properties { get; set; }
    }

    private class PropertyData
    {
        public string? Name { get; set; }
        public string? Type { get; set; }
        public object? Value { get; set; }
    }
}
