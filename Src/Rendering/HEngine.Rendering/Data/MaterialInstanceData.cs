using System.Numerics;
using System.Runtime.InteropServices;

namespace HEngine.Rendering.Data;

[StructLayout(LayoutKind.Sequential, Pack = 16)]
public struct MaterialInstanceData
{
    public Vector4 DiffuseColor;
    public Vector4 SpecularColor;
    public Vector4 EmissiveColor;
    public float Metallic;
    public float Roughness;
    public float Transparency;
    public float EmissiveIntensity;
    public Vector4 CustomData1;
    public Vector4 CustomData2;

    public static readonly int SizeInBytes = Marshal.SizeOf<MaterialInstanceData>();

    public MaterialInstanceData()
    {
        DiffuseColor = Vector4.One;
        SpecularColor = new Vector4(1, 1, 1, 32);
        EmissiveColor = Vector4.Zero;
        Metallic = 0.0f;
        Roughness = 0.5f;
        Transparency = 1.0f;
        EmissiveIntensity = 0.0f;
        CustomData1 = Vector4.Zero;
        CustomData2 = Vector4.Zero;
    }

    public static MaterialInstanceData FromMaterial(Material material)
    {
        return new MaterialInstanceData
        {
            DiffuseColor = material.DiffuseColor,
            SpecularColor = material.SpecularColor,
            EmissiveColor = material.GetVector4("_EmissiveColor"),
            Metallic = material.Metallic,
            Roughness = material.Roughness,
            Transparency = material.GetFloat("_Transparency", 1.0f),
            EmissiveIntensity = material.GetFloat("_EmissiveIntensity", 0.0f),
            CustomData1 = material.GetVector4("_CustomData1"),
            CustomData2 = material.GetVector4("_CustomData2")
        };
    }

    public static MaterialInstanceData FromMaterialInstance(MaterialInstance instance)
    {
        return new MaterialInstanceData
        {
            DiffuseColor = instance.DiffuseColor,
            SpecularColor = instance.SpecularColor,
            EmissiveColor = instance.GetVector4("_EmissiveColor"),
            Metallic = instance.Metallic,
            Roughness = instance.Roughness,
            Transparency = instance.GetFloat("_Transparency", 1.0f),
            EmissiveIntensity = instance.GetFloat("_EmissiveIntensity", 0.0f),
            CustomData1 = instance.GetVector4("_CustomData1"),
            CustomData2 = instance.GetVector4("_CustomData2")
        };
    }

    public readonly int GetAlignedSize()
    {
        return (SizeInBytes + 255) & ~255;
    }
}
