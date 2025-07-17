using HEngine.Core.Components.Physics;
using System.Numerics;

namespace HEngine.Core.Tests.Components.Physics;

public class CollisionInfoTests {
    [Fact]
    public void Constructor_InitializesCorrectly()
    {
        uint entityId = 123;
        var contactPoint = new Vector3(1f, 2f, 3f);
        var normal = new Vector3(0f, 1f, 0f);
        var penetration = 0.5f;

        var collisionInfo = new CollisionInfo(entityId, contactPoint, normal, penetration);

        Assert.Equal(entityId, collisionInfo.OtherEntityId);
        Assert.Equal(contactPoint, collisionInfo.ContactPoint);
        Assert.Equal(normal, collisionInfo.ContactNormal);
        Assert.Equal(penetration, collisionInfo.Penetration);
        Assert.Equal(0f, collisionInfo.Impulse);
    }

    [Fact]
    public void Properties_CanBeModified()
    {
        var collisionInfo = new CollisionInfo();
        uint newEntityId = 456;
        var newContactPoint = new Vector3(4f, 5f, 6f);
        var newNormal = new Vector3(1f, 0f, 0f);
        var newPenetration = 1.2f;
        var newImpulse = 2.5f;

        collisionInfo.OtherEntityId = newEntityId;
        collisionInfo.ContactPoint = newContactPoint;
        collisionInfo.ContactNormal = newNormal;
        collisionInfo.Penetration = newPenetration;
        collisionInfo.Impulse = newImpulse;

        Assert.Equal(newEntityId, collisionInfo.OtherEntityId);
        Assert.Equal(newContactPoint, collisionInfo.ContactPoint);
        Assert.Equal(newNormal, collisionInfo.ContactNormal);
        Assert.Equal(newPenetration, collisionInfo.Penetration);
        Assert.Equal(newImpulse, collisionInfo.Impulse);
    }
}