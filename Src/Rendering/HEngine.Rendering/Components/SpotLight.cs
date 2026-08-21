using HEngine.Core.Contracts;
using System.Numerics;

namespace HEngine.Rendering.Components;

public struct SpotLight : IComponent {
    public Vector3 Color;
    public float Intensity;
    public Vector3 Direction;
    public float Range;
    public float InnerConeAngle;
    public float OuterConeAngle;

    public SpotLight(
        Vector3 direction,
        Vector3 color,
        float intensity = 1f,
        float range = 10f,
        float innerAngle = 30f,
        float outerAngle = 45f)
    {
        Direction = Vector3.Normalize(direction);
        Color = color;
        Intensity = intensity;
        Range = range;
        InnerConeAngle = MathF.PI * innerAngle / 180f;
        OuterConeAngle = MathF.PI * outerAngle / 180f;
    }
}