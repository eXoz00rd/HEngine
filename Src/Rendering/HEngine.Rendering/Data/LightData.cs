using System.Numerics;

namespace HEngine.Core.Rendering.Data;

public enum LightType
{
    Directional = 0,
    Point = 1,
    Spot = 2,
}

public readonly struct LightData
{
    public required LightType Type { get; init; }

    public required Vector3 Color { get; init; }
    public required float Intensity { get; init; }

    public Vector3 Direction { get; init; }

    public Vector3 Position { get; init; }
    public float Range { get; init; }
    public float Attenuation { get; init; }

    public float InnerConeAngle { get; init; }
    public float OuterConeAngle { get; init; }
}
