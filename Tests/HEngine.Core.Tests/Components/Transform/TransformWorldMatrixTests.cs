using System.Numerics;
using HEngine.Core.Managers;
using Xunit;

namespace HEngine.Core.Tests.Components.Transform;

public class TransformWorldMatrixTests
{
    [Fact]
    public void GetWorldMatrix_NoParent_EqualsLocalMatrix()
    {
        using var world = new WorldManager();
        var e = world.CreateEntity();
        world.AddComponent(e, new HEngine.Core.Components.Transform.Transform(new Vector3(3, 4, 5), Quaternion.CreateFromYawPitchRoll(0.1f, 0.2f, 0.3f), new Vector3(2, 2, 2)));

        ref var t = ref world.GetComponent<HEngine.Core.Components.Transform.Transform>(e);
        var local = t.ToMatrix();
        var worldM = t.GetWorldMatrix(world);

        Assert.Equal(local, worldM);
    }
}
