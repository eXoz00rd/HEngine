using HEngine.Core.Contracts;
using System.Numerics;

namespace HEngine.Core.Components.Physics;

/// <summary>
/// Collider box/sześcian
/// </summary>
public struct BoxCollider : IComponent
{
    public Vector3 Size;
    public Vector3 Center;
    
    public BoxCollider(Vector3 size, Vector3 center = default)
    {
        Size = size;
        Center = center;
    }
}