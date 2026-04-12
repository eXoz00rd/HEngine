using System;
using System.Collections.Generic;
using System.Text;
using HEngine.Rendering.Enums;

namespace HEngine.Rendering.Data;

public readonly struct ShaderVariant : IEquatable<ShaderVariant>
{
    public ShaderFeatureFlags Features { get; }

    public ShaderVariant(ShaderFeatureFlags features)
    {
        Features = features;
    }

    public bool HasFeature(ShaderFeatureFlags feature)
    {
        return (Features & feature) == feature;
    }

    public Dictionary<string, string> GetDefines()
    {
        var defines = new Dictionary<string, string>();

        if (HasFeature(ShaderFeatureFlags.UseNormalMap))
            defines["USE_NORMAL_MAP"] = "1";

        if (HasFeature(ShaderFeatureFlags.UseSpecularMap))
            defines["USE_SPECULAR_MAP"] = "1";

        if (HasFeature(ShaderFeatureFlags.UseEmissiveMap))
            defines["USE_EMISSIVE_MAP"] = "1";

        if (HasFeature(ShaderFeatureFlags.UseAlphaTest))
            defines["USE_ALPHA_TEST"] = "1";

        if (HasFeature(ShaderFeatureFlags.UseSkinning))
            defines["USE_SKINNING"] = "1";

        if (HasFeature(ShaderFeatureFlags.UseVertexColors))
            defines["USE_VERTEX_COLORS"] = "1";

        if (HasFeature(ShaderFeatureFlags.UseInstancing))
            defines["USE_INSTANCING"] = "1";

        if (HasFeature(ShaderFeatureFlags.UseFog))
            defines["USE_FOG"] = "1";

        if (HasFeature(ShaderFeatureFlags.UseShadows))
            defines["USE_SHADOWS"] = "1";

        if (HasFeature(ShaderFeatureFlags.UseAmbientOcclusion))
            defines["USE_AMBIENT_OCCLUSION"] = "1";

        if (HasFeature(ShaderFeatureFlags.UseMetallicRoughness))
            defines["USE_METALLIC_ROUGHNESS"] = "1";

        if (HasFeature(ShaderFeatureFlags.UseParallaxMapping))
            defines["USE_PARALLAX_MAPPING"] = "1";

        return defines;
    }

    public string GetVariantKey()
    {
        return ((uint)Features).ToString("X8");
    }

    public string GetVariantName()
    {
        if (Features == ShaderFeatureFlags.None)
            return "Base";

        var sb = new StringBuilder();
        var features = Features;

        if ((features & ShaderFeatureFlags.UseNormalMap) != 0)
            sb.Append("_NormalMap");
        if ((features & ShaderFeatureFlags.UseSpecularMap) != 0)
            sb.Append("_SpecularMap");
        if ((features & ShaderFeatureFlags.UseEmissiveMap) != 0)
            sb.Append("_EmissiveMap");
        if ((features & ShaderFeatureFlags.UseAlphaTest) != 0)
            sb.Append("_AlphaTest");
        if ((features & ShaderFeatureFlags.UseSkinning) != 0)
            sb.Append("_Skinning");
        if ((features & ShaderFeatureFlags.UseVertexColors) != 0)
            sb.Append("_VertexColors");
        if ((features & ShaderFeatureFlags.UseInstancing) != 0)
            sb.Append("_Instancing");
        if ((features & ShaderFeatureFlags.UseFog) != 0)
            sb.Append("_Fog");
        if ((features & ShaderFeatureFlags.UseShadows) != 0)
            sb.Append("_Shadows");
        if ((features & ShaderFeatureFlags.UseAmbientOcclusion) != 0)
            sb.Append("_AO");
        if ((features & ShaderFeatureFlags.UseMetallicRoughness) != 0)
            sb.Append("_MetallicRoughness");
        if ((features & ShaderFeatureFlags.UseParallaxMapping) != 0)
            sb.Append("_Parallax");

        return sb.Length > 0 ? sb.ToString().TrimStart('_') : "Base";
    }

    public bool Equals(ShaderVariant other)
    {
        return Features == other.Features;
    }

    public override bool Equals(object? obj)
    {
        return obj is ShaderVariant other && Equals(other);
    }

    public override int GetHashCode()
    {
        return (int)Features;
    }

    public static bool operator ==(ShaderVariant left, ShaderVariant right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ShaderVariant left, ShaderVariant right)
    {
        return !left.Equals(right);
    }
}
