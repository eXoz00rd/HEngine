using HEngine.Core.Contracts;
using HEngine.Core.Primitives;
using System.Numerics;

namespace HEngine.Core.Components.Physics;

public struct CollisionInfo : IComponent {
    public uint OtherEntityId;
    public Vector3 ContactPoint;
    public Vector3 ContactNormal;
    public float Penetration;
    public float Impulse;

    public CollisionInfo(uint otherEntityId, Vector3 contactPoint, Vector3 normal, float penetration)
    {
        OtherEntityId = otherEntityId;
        ContactPoint = contactPoint;
        ContactNormal = normal;
        Penetration = penetration;
        Impulse = 0f;
    }
}