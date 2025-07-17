using HEngine.Core.Contracts;
using System.Numerics;

namespace HEngine.Core.Components.Rendering;

public struct PointLight : IComponent {
    public Vector3 Color;
    public float Intensity;
    public float Range;
    public float Attenuation;

    public PointLight(Vector3 color, float intensity = 1f, float range = 10f, float attenuation = 1f)
    {
        Color = color;
        Intensity = MathF.Max(0f, intensity);
        Range = MathF.Max(0f, range);
        Attenuation = MathF.Max(0f, attenuation);
    }
}