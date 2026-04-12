using System.Numerics;
using HEngine.Rendering.Data;

namespace HEngine.Rendering.Managers;

/// <summary>
/// Converts a Material's property block into PBRMaterialConstants for GPU upload.
/// </summary>
public static class MaterialConstantsSerializer
{
    public static PBRMaterialConstants ToGpu(Material material)
    {
        ArgumentNullException.ThrowIfNull(material);

        return new PBRMaterialConstants
        {
            DiffuseColor = material.DiffuseColor,
            Metallic = material.Metallic,
            Roughness = material.Roughness,
            AO = material.GetFloat("_AO", 1.0f),
            EmissiveIntensity = material.GetFloat("_EmissiveIntensity", 0.0f),
            EmissiveColor = material.GetVector4("_EmissiveColor")
        };
    }

    public static PBRMaterialConstants Default()
    {
        return new PBRMaterialConstants
        {
            DiffuseColor = Vector4.One,
            Metallic = 0.0f,
            Roughness = 0.5f,
            AO = 1.0f,
            EmissiveIntensity = 0.0f,
            EmissiveColor = Vector4.Zero
        };
    }
}

