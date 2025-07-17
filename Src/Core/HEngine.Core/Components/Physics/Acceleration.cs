using HEngine.Core.Contracts;
using System.Numerics;

namespace HEngine.Core.Components.Physics;

/// <summary>
/// Komponent przyspieszenia/sił
/// </summary>
public struct Acceleration : IComponent
{
    public Vector3 Linear;
    public Vector3 Angular;
    
    public Acceleration(Vector3 linear = default, Vector3 angular = default)
    {
        Linear = linear;
        Angular = angular;
    }
}