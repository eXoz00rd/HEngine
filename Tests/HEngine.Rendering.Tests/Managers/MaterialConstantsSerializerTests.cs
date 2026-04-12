using System.Numerics;
using HEngine.Rendering.Data;
using HEngine.Rendering.Managers;

namespace HEngine.Rendering.Tests.Managers;

public class MaterialConstantsSerializerTests
{
    [Fact(DisplayName = "ToGpu serializes metallic material correctly")]
    public void ToGpu_Metallic_Material()
    {
        var material = new Material
        {
            DiffuseColor = new Vector4(0.9f, 0.9f, 0.9f, 1.0f),
            Metallic = 1.0f,
            Roughness = 0.2f
        };
        material.SetProperty("_AO", 1.0f);

        var constants = MaterialConstantsSerializer.ToGpu(material);

        Assert.Equal(new Vector4(0.9f, 0.9f, 0.9f, 1.0f), constants.DiffuseColor);
        Assert.Equal(1.0f, constants.Metallic);
        Assert.Equal(0.2f, constants.Roughness);
        Assert.Equal(1.0f, constants.AO);
    }

    [Fact(DisplayName = "ToGpu serializes dielectric material correctly")]
    public void ToGpu_Dielectric_Material()
    {
        var material = new Material
        {
            DiffuseColor = Vector4.One,
            Metallic = 0.0f,
            Roughness = 0.8f
        };

        var constants = MaterialConstantsSerializer.ToGpu(material);

        Assert.Equal(0.0f, constants.Metallic);
        Assert.Equal(0.8f, constants.Roughness);
    }

    [Fact(DisplayName = "ToGpu serializes emissive properties")]
    public void ToGpu_Emissive_Properties()
    {
        var material = new Material();
        material.SetProperty("_EmissiveIntensity", 3.0f);
        material.SetProperty("_EmissiveColor", new Vector4(1f, 0.5f, 0f, 1f));

        var constants = MaterialConstantsSerializer.ToGpu(material);

        Assert.Equal(3.0f, constants.EmissiveIntensity);
        Assert.Equal(new Vector4(1f, 0.5f, 0f, 1f), constants.EmissiveColor);
    }

    [Fact(DisplayName = "Default returns sensible PBR defaults")]
    public void Default_Returns_Sensible_Defaults()
    {
        var constants = MaterialConstantsSerializer.Default();

        Assert.Equal(Vector4.One, constants.DiffuseColor);
        Assert.Equal(0.0f, constants.Metallic);
        Assert.Equal(0.5f, constants.Roughness);
        Assert.Equal(1.0f, constants.AO);
        Assert.Equal(0.0f, constants.EmissiveIntensity);
    }

    [Fact(DisplayName = "ToGpu with all 14 material presets does not throw")]
    public void ToGpu_All_Presets_Do_Not_Throw()
    {
        var library = new MaterialLibrary();

        foreach (var (_, template) in library.Templates)
        {
            var material = template.Instantiate();
            var constants = MaterialConstantsSerializer.ToGpu(material);
            Assert.True(constants.Roughness >= 0f && constants.Roughness <= 1f);
            Assert.True(constants.Metallic >= 0f && constants.Metallic <= 1f);
        }
    }
}

