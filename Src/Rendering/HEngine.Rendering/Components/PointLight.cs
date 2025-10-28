using System.Numerics;
using HEngine.Core.Contracts;

namespace HEngine.Rendering.Components;

public struct PointLight : IComponent
{
    public float Radius;
    public Vector4 Color;
    public bool Enabled;

    public PointLight(float radius, Vector4 color)
    {
        Radius = radius;
        Color = color;
        Enabled = true;
    }

    public static PointLight Default => new()
    {
        Radius = 10.0f,
        Color = new Vector4(1.0f, 1.0f, 1.0f, 1.0f),
        Enabled = true
    };
}
