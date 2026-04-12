using System.Numerics;
using HEngine.Rendering.Data;
using HEngine.Rendering.Enums;

namespace HEngine.Rendering.Managers;

public sealed class MaterialLibrary
{
    private readonly Dictionary<string, MaterialTemplate> _templates = new();
    private readonly Dictionary<MaterialPreset, string> _presetMapping = new();
    private bool _initialized;

    public IReadOnlyDictionary<string, MaterialTemplate> Templates => _templates;

    public MaterialLibrary()
    {
        InitializeBuiltInPresets();
    }

    public void RegisterTemplate(MaterialTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);

        if (string.IsNullOrWhiteSpace(template.Name))
            throw new ArgumentException("Template name cannot be empty", nameof(template));

        _templates[template.Name] = template;
    }

    public void RegisterPreset(MaterialPreset preset, string templateName)
    {
        if (!_templates.ContainsKey(templateName))
            throw new ArgumentException($"Template '{templateName}' not found", nameof(templateName));

        _presetMapping[preset] = templateName;
    }

    public bool TryGetTemplate(string name, out MaterialTemplate? template)
    {
        return _templates.TryGetValue(name, out template);
    }

    public MaterialTemplate? GetTemplate(string name)
    {
        return _templates.GetValueOrDefault(name);
    }

    public Material CreateMaterial(string templateName)
    {
        if (!_templates.TryGetValue(templateName, out var template))
            throw new ArgumentException($"Template '{templateName}' not found", nameof(templateName));

        return template.Instantiate();
    }

    public Material CreateMaterial(MaterialPreset preset)
    {
        if (!_presetMapping.TryGetValue(preset, out var templateName))
            throw new ArgumentException($"Preset '{preset}' not mapped to any template", nameof(preset));

        return CreateMaterial(templateName);
    }

    public bool HasTemplate(string name)
    {
        return _templates.ContainsKey(name);
    }

    public bool HasPreset(MaterialPreset preset)
    {
        return _presetMapping.ContainsKey(preset);
    }

    public void RemoveTemplate(string name)
    {
        _templates.Remove(name);
    }

    public void Clear()
    {
        _templates.Clear();
        _presetMapping.Clear();
        _initialized = false;
    }

    private void InitializeBuiltInPresets()
    {
        if (_initialized)
            return;

        CreateStandardTemplate();
        CreateMetallicTemplate();
        CreateDielectricTemplate();
        CreateGlassTemplate();
        CreateEmissiveTemplate();
        CreateTransparentTemplate();
        CreateRoughTemplate();
        CreateSmoothTemplate();
        CreatePlasticTemplate();
        CreateSkinTemplate();
        CreateFabricTemplate();
        CreateWoodTemplate();
        CreateStoneTemplate();
        CreateMetalTemplate();

        _initialized = true;
    }

    private void CreateStandardTemplate()
    {
        var template = new MaterialTemplate("Standard", "Standard PBR material with balanced properties");
        template.Properties.SetColor("_DiffuseColor", Vector4.One);
        template.Properties.SetColor("_SpecularColor", new Vector4(1, 1, 1, 32));
        template.Properties.SetFloat("_Metallic", 0.0f);
        template.Properties.SetFloat("_Roughness", 0.5f);
        RegisterTemplate(template);
        RegisterPreset(MaterialPreset.Standard, "Standard");
    }

    private void CreateMetallicTemplate()
    {
        var template = new MaterialTemplate("Metallic", "Fully metallic material with high reflectivity");
        template.Properties.SetColor("_DiffuseColor", new Vector4(0.9f, 0.9f, 0.9f, 1.0f));
        template.Properties.SetColor("_SpecularColor", Vector4.One);
        template.Properties.SetFloat("_Metallic", 1.0f);
        template.Properties.SetFloat("_Roughness", 0.2f);
        RegisterTemplate(template);
        RegisterPreset(MaterialPreset.Metallic, "Metallic");
    }

    private void CreateDielectricTemplate()
    {
        var template = new MaterialTemplate("Dielectric", "Non-metallic material with low reflectivity");
        template.Properties.SetColor("_DiffuseColor", Vector4.One);
        template.Properties.SetColor("_SpecularColor", new Vector4(0.04f, 0.04f, 0.04f, 1.0f));
        template.Properties.SetFloat("_Metallic", 0.0f);
        template.Properties.SetFloat("_Roughness", 0.7f);
        RegisterTemplate(template);
        RegisterPreset(MaterialPreset.Dielectric, "Dielectric");
    }

    private void CreateGlassTemplate()
    {
        var template = new MaterialTemplate("Glass", "Transparent glass material");
        template.Properties.SetColor("_DiffuseColor", new Vector4(0.95f, 0.95f, 0.95f, 0.1f));
        template.Properties.SetColor("_SpecularColor", Vector4.One);
        template.Properties.SetFloat("_Metallic", 0.0f);
        template.Properties.SetFloat("_Roughness", 0.0f);
        template.Properties.SetFloat("_Transparency", 0.9f);
        template.Properties.SetFloat("_RefractiveIndex", 1.5f);
        RegisterTemplate(template);
        RegisterPreset(MaterialPreset.Glass, "Glass");
    }

    private void CreateEmissiveTemplate()
    {
        var template = new MaterialTemplate("Emissive", "Self-illuminating emissive material");
        template.Properties.SetColor("_DiffuseColor", Vector4.One);
        template.Properties.SetColor("_EmissiveColor", new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
        template.Properties.SetFloat("_EmissiveIntensity", 2.0f);
        template.Properties.SetFloat("_Metallic", 0.0f);
        template.Properties.SetFloat("_Roughness", 1.0f);
        RegisterTemplate(template);
        RegisterPreset(MaterialPreset.Emissive, "Emissive");
    }

    private void CreateTransparentTemplate()
    {
        var template = new MaterialTemplate("Transparent", "Semi-transparent material");
        template.Properties.SetColor("_DiffuseColor", new Vector4(1.0f, 1.0f, 1.0f, 0.5f));
        template.Properties.SetColor("_SpecularColor", new Vector4(0.5f, 0.5f, 0.5f, 1.0f));
        template.Properties.SetFloat("_Metallic", 0.0f);
        template.Properties.SetFloat("_Roughness", 0.3f);
        template.Properties.SetFloat("_Transparency", 0.5f);
        RegisterTemplate(template);
        RegisterPreset(MaterialPreset.Transparent, "Transparent");
    }

    private void CreateRoughTemplate()
    {
        var template = new MaterialTemplate("Rough", "Rough matte material with diffuse appearance");
        template.Properties.SetColor("_DiffuseColor", new Vector4(0.7f, 0.7f, 0.7f, 1.0f));
        template.Properties.SetColor("_SpecularColor", new Vector4(0.02f, 0.02f, 0.02f, 1.0f));
        template.Properties.SetFloat("_Metallic", 0.0f);
        template.Properties.SetFloat("_Roughness", 1.0f);
        RegisterTemplate(template);
        RegisterPreset(MaterialPreset.Rough, "Rough");
    }

    private void CreateSmoothTemplate()
    {
        var template = new MaterialTemplate("Smooth", "Smooth polished material with sharp reflections");
        template.Properties.SetColor("_DiffuseColor", new Vector4(0.9f, 0.9f, 0.9f, 1.0f));
        template.Properties.SetColor("_SpecularColor", Vector4.One);
        template.Properties.SetFloat("_Metallic", 0.5f);
        template.Properties.SetFloat("_Roughness", 0.05f);
        RegisterTemplate(template);
        RegisterPreset(MaterialPreset.Smooth, "Smooth");
    }

    private void CreatePlasticTemplate()
    {
        var template = new MaterialTemplate("Plastic", "Glossy plastic material");
        template.Properties.SetColor("_DiffuseColor", new Vector4(0.8f, 0.1f, 0.1f, 1.0f));
        template.Properties.SetColor("_SpecularColor", new Vector4(0.5f, 0.5f, 0.5f, 1.0f));
        template.Properties.SetFloat("_Metallic", 0.0f);
        template.Properties.SetFloat("_Roughness", 0.3f);
        RegisterTemplate(template);
        RegisterPreset(MaterialPreset.Plastic, "Plastic");
    }

    private void CreateSkinTemplate()
    {
        var template = new MaterialTemplate("Skin", "Subsurface scattering skin material");
        template.Properties.SetColor("_DiffuseColor", new Vector4(0.95f, 0.75f, 0.65f, 1.0f));
        template.Properties.SetColor("_SpecularColor", new Vector4(0.2f, 0.2f, 0.2f, 1.0f));
        template.Properties.SetFloat("_Metallic", 0.0f);
        template.Properties.SetFloat("_Roughness", 0.6f);
        template.Properties.SetFloat("_SubsurfaceScattering", 0.5f);
        RegisterTemplate(template);
        RegisterPreset(MaterialPreset.Skin, "Skin");
    }

    private void CreateFabricTemplate()
    {
        var template = new MaterialTemplate("Fabric", "Soft fabric material with anisotropic reflections");
        template.Properties.SetColor("_DiffuseColor", new Vector4(0.4f, 0.4f, 0.6f, 1.0f));
        template.Properties.SetColor("_SpecularColor", new Vector4(0.1f, 0.1f, 0.1f, 1.0f));
        template.Properties.SetFloat("_Metallic", 0.0f);
        template.Properties.SetFloat("_Roughness", 0.9f);
        template.Properties.SetFloat("_Anisotropy", 0.7f);
        RegisterTemplate(template);
        RegisterPreset(MaterialPreset.Fabric, "Fabric");
    }

    private void CreateWoodTemplate()
    {
        var template = new MaterialTemplate("Wood", "Natural wood material");
        template.Properties.SetColor("_DiffuseColor", new Vector4(0.6f, 0.4f, 0.2f, 1.0f));
        template.Properties.SetColor("_SpecularColor", new Vector4(0.15f, 0.15f, 0.15f, 1.0f));
        template.Properties.SetFloat("_Metallic", 0.0f);
        template.Properties.SetFloat("_Roughness", 0.7f);
        RegisterTemplate(template);
        RegisterPreset(MaterialPreset.Wood, "Wood");
    }

    private void CreateStoneTemplate()
    {
        var template = new MaterialTemplate("Stone", "Rough stone material");
        template.Properties.SetColor("_DiffuseColor", new Vector4(0.5f, 0.5f, 0.5f, 1.0f));
        template.Properties.SetColor("_SpecularColor", new Vector4(0.05f, 0.05f, 0.05f, 1.0f));
        template.Properties.SetFloat("_Metallic", 0.0f);
        template.Properties.SetFloat("_Roughness", 0.95f);
        RegisterTemplate(template);
        RegisterPreset(MaterialPreset.Stone, "Stone");
    }

    private void CreateMetalTemplate()
    {
        var template = new MaterialTemplate("Metal", "Polished metal material");
        template.Properties.SetColor("_DiffuseColor", new Vector4(0.8f, 0.8f, 0.8f, 1.0f));
        template.Properties.SetColor("_SpecularColor", Vector4.One);
        template.Properties.SetFloat("_Metallic", 1.0f);
        template.Properties.SetFloat("_Roughness", 0.15f);
        RegisterTemplate(template);
        RegisterPreset(MaterialPreset.Metal, "Metal");
    }
}
