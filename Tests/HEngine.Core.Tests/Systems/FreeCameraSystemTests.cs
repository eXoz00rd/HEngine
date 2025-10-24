using System.Numerics;
using HEngine.Core.Components.Rendering;
using HEngine.Core.Contracts;
using HEngine.Core.Managers;
using HEngine.Core.Systems;

namespace HEngine.Core.Tests.Systems;

public class FreeCameraSystemTests
{
    private sealed class FakeInput(Vector3 move, Vector2 look) : ICameraInputProvider
    {
        public Vector3 Movement = move;
        public Vector2 Look = look;
        public Vector3 GetMovementAxes() => Movement;
        public Vector2 GetLookDelta() => Look;
    }

    [Fact(DisplayName = "FreeCameraSystem translates camera forward with positive Z movement")]
    public void Update_Moves_Forward()
    {
        var world = new WorldManager();
        var entity = world.CreateEntity();
        world.AddComponent(entity, new Camera
        {
            Position = new Vector3(0, 0, 5),
            Target = Vector3.Zero,
            Up = Vector3.UnitY
        });

        var input = new FakeInput(new Vector3(0, 0, 1), Vector2.Zero);
        var system = new FreeCameraSystem(input)
        {
            MoveSpeed = 2f,
            LookSpeed = 1f,
            Enabled = true
        };

        system.Initialize(world);
        system.Update(1f);

        var query = world.CreateQuery<Camera>();
        query.TryGetFirst(out var _, out var cam);
        
        Assert.Equal(new Vector3(0, 0, 3), cam.Position);
        Assert.Equal(new Vector3(0, 0, -2), cam.Target);
    }

    [Fact(DisplayName = "FreeCameraSystem yaw rotates camera around Up vector")]
    public void Update_Yaw_Rotates_View()
    {
        var world = new WorldManager();
        var entity = world.CreateEntity();
        world.AddComponent(entity, new Camera
        {
            Position = new Vector3(0, 0, 5),
            Target = Vector3.Zero,
            Up = Vector3.UnitY
        });
        
        var input = new FakeInput(Vector3.Zero, new Vector2(1, 0));
        var system = new FreeCameraSystem(input)
        {
            MoveSpeed = 0f,
            LookSpeed = MathF.PI / 2f,
            Enabled = true
        };

        system.Initialize(world);
        system.Update(1f);

        var query = world.CreateQuery<Camera>();
        query.TryGetFirst(out var _, out var cam);
        
        var expectedTarget = cam.Position + new Vector3(-1, 0, 0) * 5f;
        Assert.True(Vector3.Distance(expectedTarget, cam.Target) < 1e-4f);
    }

    [Fact(DisplayName = "FreeCameraSystem disabled does not change camera")]
    public void Disabled_Does_Not_Update()
    {
        var world = new WorldManager();
        var entity = world.CreateEntity();
        world.AddComponent(entity, new Camera
        {
            Position = new Vector3(0, 0, 5),
            Target = Vector3.Zero,
            Up = Vector3.UnitY
        });

        var input = new FakeInput(new Vector3(1, 2, 3), new Vector2(4, 5));
        var system = new FreeCameraSystem(input)
        {
            Enabled = false
        };

        system.Initialize(world);
        system.Update(1f);

        var query = world.CreateQuery<Camera>();
        query.TryGetFirst(out var _, out var cam);

        Assert.Equal(new Vector3(0, 0, 5), cam.Position);
        Assert.Equal(Vector3.Zero, cam.Target);
        Assert.Equal(Vector3.UnitY, cam.Up);
    }

    [Fact(DisplayName = "FreeCameraSystem moves up/down with Y axis and respects MoveSpeed and deltaTime")]
    public void Update_Moves_Up_With_Speed_And_DeltaTime()
    {
        var world = new WorldManager();
        var entity = world.CreateEntity();
        world.AddComponent(entity, new Camera
        {
            Position = new Vector3(0, 0, 5),
            Target = Vector3.Zero,
            Up = Vector3.UnitY
        });

        var input = new FakeInput(new Vector3(0, 1, 0), Vector2.Zero);
        var system = new FreeCameraSystem(input)
        {
            MoveSpeed = 3f,
            LookSpeed = 1f,
            Enabled = true
        };

        system.Initialize(world);
        system.Update(2f);

        var query = world.CreateQuery<Camera>();
        query.TryGetFirst(out var _, out var cam);

        Assert.Equal(new Vector3(0, 6, 5), cam.Position);
        Assert.Equal(new Vector3(0, 6, 0), cam.Target);
    }
}