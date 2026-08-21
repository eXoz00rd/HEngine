using HEngine.Core.Contracts;
using System.Numerics;

namespace HEngine.Rendering.Components;

public struct DirectionalLight : IComponent
{
    public Vector3 Color;
    public float Intensity;
    public Vector3 Direction;
    
    public DirectionalLight(Vector3 direction, Vector3 color, float intensity = 1f)
    {
        Direction = Vector3.Normalize(direction);
        Color = color;
        Intensity = intensity;
    }
}