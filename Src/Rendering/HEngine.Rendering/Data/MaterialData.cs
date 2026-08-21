using System.Numerics;

namespace HEngine.Core.Rendering.Data;

public readonly struct MaterialData
{
    public MaterialData() { }

    public required Vector4 DiffuseColor { get; init; }
    public float Metallic { get; init; }
    public float Roughness { get; init; }
    public float AO { get; init; }
    public Vector4 EmissiveColor { get; init; }
    public float EmissiveIntensity { get; init; }

    public int DiffuseTextureHandle { get; init; } = -1;

    public int NormalTextureHandle { get; init; } = -1;
    public int MetallicRoughnessTextureHandle { get; init; } = -1;
    public int EmissiveTextureHandle { get; init; } = -1;
    public int AOTextureHandle { get; init; } = -1;
}
