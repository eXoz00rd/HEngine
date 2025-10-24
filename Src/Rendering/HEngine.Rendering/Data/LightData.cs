using System.Numerics;

namespace HEngine.Rendering.Data;

public enum LightType
{
    Directional = 0,
    Point = 1,
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
}