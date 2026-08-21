using System.Numerics;
using HEngine.Rendering.Data;
using HEngine.Rendering.Serialization;

namespace HEngine.Rendering.Tests.Serialization;

public class MaterialTemplateSerializerTests
{
    [Fact(DisplayName = "Round-trip preserves name, description and every property type")]
    public void RoundTrip_Preserves_Name_Description_And_Properties()
    {
        var template = new MaterialTemplate("Rusty Metal", "A worn metallic surface");
        template.Properties.SetFloat("_Roughness", 0.4f);
        template.Properties.SetInt("_LayerIndex", 3);
        template.Properties.SetVector2("_UvTiling", new Vector2(2f, 4f));
        template.Properties.SetVector3("_Emission", new Vector3(0.1f, 0.2f, 0.3f));
        template.Properties.SetVector4("_Tint", new Vector4(1f, 0.5f, 0.25f, 1f));
        template.Properties.SetMatrix("_UvTransform", Matrix4x4.CreateScale(2f));
        template.Properties.SetTexture("_DiffuseMap", "textures/rust_diffuse.png");
        template.Properties.SetCubeTexture("_EnvironmentMap", "textures/env.dds");

        var json = MaterialTemplateSerializer.SerializeToJson(template);
        var loaded = MaterialTemplateSerializer.DeserializeFromJson(json);

        Assert.Equal(template.Name, loaded.Name);
        Assert.Equal(template.Description, loaded.Description);
        Assert.Equal(0.4f, loaded.Properties.GetFloat("_Roughness"));
        Assert.Equal(3, loaded.Properties.GetInt("_LayerIndex"));
        Assert.Equal(new Vector2(2f, 4f), loaded.Properties.GetVector2("_UvTiling"));
        Assert.Equal(new Vector3(0.1f, 0.2f, 0.3f), loaded.Properties.GetVector3("_Emission"));
        Assert.Equal(new Vector4(1f, 0.5f, 0.25f, 1f), loaded.Properties.GetVector4("_Tint"));
        Assert.Equal(Matrix4x4.CreateScale(2f), loaded.Properties.GetMatrix("_UvTransform"));
        Assert.Equal("textures/rust_diffuse.png", loaded.Properties.GetTexture("_DiffuseMap"));
        Assert.Equal("textures/env.dds", loaded.Properties.GetTexture("_EnvironmentMap"));
    }

    [Fact(DisplayName = "Round-trip preserves an empty template")]
    public void RoundTrip_Preserves_Empty_Template()
    {
        var template = new MaterialTemplate("Empty", string.Empty);

        var json = MaterialTemplateSerializer.SerializeToJson(template);
        var loaded = MaterialTemplateSerializer.DeserializeFromJson(json);

        Assert.Equal("Empty", loaded.Name);
        Assert.Equal(0, loaded.Properties.PropertyCount);
    }

    [Fact(DisplayName = "SaveToFile then LoadFromFile round-trips a template")]
    public void SaveToFile_Then_LoadFromFile_RoundTrips()
    {
        var template = new MaterialTemplate("Disk Material", "Persisted to disk");
        template.Properties.SetFloat("_Metallic", 0.75f);

        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");
        try
        {
            MaterialTemplateSerializer.SaveToFile(template, path);
            var loaded = MaterialTemplateSerializer.LoadFromFile(path);

            Assert.Equal(template.Name, loaded.Name);
            Assert.Equal(0.75f, loaded.Properties.GetFloat("_Metallic"));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
