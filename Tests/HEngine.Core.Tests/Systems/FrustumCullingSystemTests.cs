using System.Numerics;
using HEngine.Core.Components.Rendering;
using HEngine.Core.Components.Transform;
using HEngine.Core.Managers;
using HEngine.Core.Systems;

namespace HEngine.Core.Tests.Systems;

public class FrustumCullingSystemTests
{
    private static WorldManager CreateWorldWithDefaultCamera()
    {
        var world = new WorldManager();
        var camEntity = world.CreateEntity();
        world.AddComponent(camEntity, new Camera
        {
            Position = new Vector3(0, 0, 5),
            Target = Vector3.Zero,
            Up = Vector3.UnitY,
            FieldOfView = MathF.PI / 2f,
            NearPlane = 0.1f,
            FarPlane = 100f,
            AspectRatio = 16f / 9f,
            IsOrthographic = false
        });
        return world;
    }

    [Fact(DisplayName = "Entity inside frustum is not culled")]
    public void Inside_Not_Culled()
    {
        var world = CreateWorldWithDefaultCamera();
        var e = world.CreateEntity();
        world.AddComponent(e, new Transform(new Vector3(0, 0, 0)));
        world.AddComponent(e, new BoundingBox(Vector3.Zero, new Vector3(0.5f)));

        var system = new FrustumCullingSystem();
        system.Initialize(world);
        system.Update(0.016f);

        Assert.False(world.HasComponent<Culled>(e));
    }

    [Fact(DisplayName = "Entity outside frustum is culled")]
    public void Outside_Is_Culled()
    {
        var world = CreateWorldWithDefaultCamera();
        var e = world.CreateEntity();
        world.AddComponent(e, new Transform(new Vector3(1000, 0, 0)));
        world.AddComponent(e, new BoundingBox(Vector3.Zero, new Vector3(0.5f)));

        var system = new FrustumCullingSystem();
        system.Initialize(world);
        system.Update(0.016f);

        Assert.True(world.HasComponent<Culled>(e));
    }

    [Fact(DisplayName = "Entity moves into frustum and culled tag is removed")]
    public void Move_Into_View_Removes_Culled()
    {
        var world = CreateWorldWithDefaultCamera();
        var e = world.CreateEntity();
        world.AddComponent(e, new Transform(new Vector3(1000, 0, 0)));
        world.AddComponent(e, new BoundingBox(Vector3.Zero, new Vector3(0.5f)));

        var system = new FrustumCullingSystem();
        system.Initialize(world);

        system.Update(0.016f);
        Assert.True(world.HasComponent<Culled>(e));
        
        ref var t = ref world.GetComponent<Transform>(e);
        t.Position = new Vector3(0, 0, -5);
        t.IsDirty = true;

        system.Update(0.016f);
        Assert.False(world.HasComponent<Culled>(e));
    }
}
