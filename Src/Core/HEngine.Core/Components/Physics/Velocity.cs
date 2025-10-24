using HEngine.Core.Contracts;
using System.Numerics;

namespace HEngine.Core.Components.Physics;

public struct Velocity : IComponent
{
    public Vector3 Linear;
    public Vector3 Angular;
    
    public Velocity(Vector3 linear = default, Vector3 angular = default)
    {
        Linear = linear;
        Angular = angular;
    }
    
    public float Speed => Linear.Length();
    public float AngularSpeed => Angular.Length();
}