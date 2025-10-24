using System.Numerics;
using HEngine.Core.Components.Transform;
using HEngine.Core.Managers;
using HEngine.Core.Primitives;
using HEngine.Core.Systems;

namespace HEngine.Core.Tests.Systems;

public class TransformHierarchySystemTests
{
    [Fact]
    public void SetParent_PreventsCycles_Throws()
    {
        using var world = new WorldManager();
        var system = new TransformHierarchySystem();
        world.AddSystem(system);

        var a = world.CreateEntity();
        var b = world.CreateEntity();
        var c = world.CreateEntity();

        world.AddComponent(a, new Transform(new Vector3(0, 0, 0)));
        world.AddComponent(b, new Transform(new Vector3(1, 0, 0)));
        world.AddComponent(c, new Transform(new Vector3(2, 0, 0)));

        system.SetParent(b, a);
        system.SetParent(c, b);

        Assert.Throws<InvalidOperationException>(() => system.SetParent(a, c));
    }

    [Fact]
    public void GetWorldMatrix_WithParentChain_ComputesExpectedWorldPosition()
    {
        using var world = new WorldManager();
        var system = new TransformHierarchySystem();
        world.AddSystem(system);

        var parent = world.CreateEntity();
        var child = world.CreateEntity();

        world.AddComponent(parent, new Transform(new Vector3(10, 0, 0)));
        world.AddComponent(child, new Transform(new Vector3(5, 0, 0)));

        system.SetParent(child, parent);
        
        world.Update(0f);

        ref var childTransform = ref world.GetComponent<Transform>(child);
        var worldMatrix = childTransform.GetWorldMatrix(world);
        
        var worldPos = new Vector3(worldMatrix.M41, worldMatrix.M42, worldMatrix.M43);
        Assert.Equal(new Vector3(15, 0, 0), worldPos);
    }

    [Fact]
    public void DirtyFlag_RecomputeOnNextUpdate()
    {
        using var world = new WorldManager();
        var system = new TransformHierarchySystem();
        world.AddSystem(system);

        var e = world.CreateEntity();
        world.AddComponent(e, new Transform(new Vector3(1, 2, 3)));

        ref var t = ref world.GetComponent<Transform>(e);
        var m1 = t.GetWorldMatrix(world);

        // Modify and mark dirty
        t.Position = new Vector3(2, 4, 6);
        t.IsDirty = true;

        world.Update(0f);
        var m2 = t.GetWorldMatrix(world);

        Assert.NotEqual(m1, m2);
        Assert.Equal(new Vector3(2, 4, 6), new Vector3(m2.M41, m2.M42, m2.M43));
    }
}
