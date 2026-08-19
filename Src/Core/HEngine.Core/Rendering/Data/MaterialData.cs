using System.Numerics;

namespace HEngine.Core.Rendering.Data;

public readonly struct MaterialData
{
    public required Vector4 DiffuseColor { get; init; }
    public float Metallic { get; init; }
    public float Roughness { get; init; }
    public float AO { get; init; }
    public Vector4 EmissiveColor { get; init; }
    public float EmissiveIntensity { get; init; }

    /// <summary>
    /// GPU texture handle for the diffuse/albedo map, or -1 when the material has no texture.
    /// </summary>
    public int DiffuseTextureHandle { get; init; }
}
