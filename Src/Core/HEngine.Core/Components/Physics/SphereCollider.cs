using HEngine.Core.Contracts;
using System.Numerics;

namespace HEngine.Core.Components.Physics;

public struct SphereCollider : IComponent
{
    public float Radius;
    public Vector3 Center;
    
    public SphereCollider(float radius, Vector3 center = default)
    {
        Radius = radius;
        Center = center;
    }
}