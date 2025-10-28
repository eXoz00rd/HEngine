using System.Numerics;
using HEngine.Core.Contracts;

namespace HEngine.Rendering.Components;

public struct DirectionalLight : IComponent
{
    public Vector3 Direction;
    public Vector4 Color;
    public bool Enabled;

    public DirectionalLight(Vector3 direction, Vector4 color)
    {
        Direction = Vector3.Normalize(direction);
        Color = color;
        Enabled = true;
    }

    public static DirectionalLight Default => new()
    {
        Direction = new Vector3(0.5f, -1.0f, 0.5f),
        Color = new Vector4(1.0f, 1.0f, 1.0f, 1.0f),
        Enabled = true
    };
}
