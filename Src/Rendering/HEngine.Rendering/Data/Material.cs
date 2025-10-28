using System.Numerics;

namespace HEngine.Rendering.Data;

public class Material
{
    public Vector4 DiffuseColor { get; set; } = Vector4.One;
    public Vector4 SpecularColor { get; set; } = new(1, 1, 1, 32);
    public float Metallic { get; set; } = 0.0f;
    public float Roughness { get; set; } = 0.5f;

    public string? DiffuseTexture { get; set; }
    public string? NormalTexture { get; set; }

    public Material()
    {
    }

    public Material(Vector4 diffuseColor)
    {
        DiffuseColor = diffuseColor;
    }

    public static Material Default => new()
    {
        DiffuseColor = Vector4.One,
        SpecularColor = new Vector4(1, 1, 1, 32),
        Metallic = 0.0f,
        Roughness = 0.5f
    };
}
