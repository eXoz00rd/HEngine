namespace HEngine.Rendering.Enums;

/// <summary>
/// Standard texture slots mapped to shader registers.
/// Each slot corresponds to a specific SRV register (t0-t7).
/// </summary>
public enum TextureSlot
{
    /// <summary>Diffuse/Albedo map → register(t0)</summary>
    DiffuseMap = 0,

    /// <summary>Normal map → register(t1)</summary>
    NormalMap = 1,

    /// <summary>Metallic-Roughness map (R=metallic, G=roughness) → register(t2)</summary>
    MetallicRoughnessMap = 2,

    /// <summary>Emissive map → register(t3)</summary>
    EmissiveMap = 3,

    /// <summary>Ambient Occlusion map → register(t4)</summary>
    AOMap = 4,

    /// <summary>Shadow map (system managed) → register(t5)</summary>
    ShadowMap = 5,

    /// <summary>Reserved for future use → register(t6)</summary>
    Custom0 = 6,

    /// <summary>Reserved for future use → register(t7)</summary>
    Custom1 = 7
}

