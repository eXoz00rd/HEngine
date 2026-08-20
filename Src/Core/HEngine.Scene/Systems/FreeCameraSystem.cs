using System.Numerics;
using HEngine.Core.Components.Rendering;
using HEngine.Core.Contracts;
using HEngine.Core.Managers;

namespace HEngine.Core.Systems;

public sealed class FreeCameraSystem : ISystem
{
    private readonly ICameraInputProvider _input;
    private WorldManager? _world;

    public bool Enabled { get; set; } = true;

    public float MoveSpeed { get; set; } = 5f;

    public float LookSpeed { get; set; } = 0.0025f;

    public FreeCameraSystem(ICameraInputProvider input)
    {
        _input = input ?? throw new ArgumentNullException(nameof(input));
    }

    public void Initialize(WorldManager worldManager)
    {
        _world = worldManager ?? throw new ArgumentNullException(nameof(worldManager));
    }

    public void Update(float deltaTime)
    {
        if (!Enabled || _world is null)
            return;

        var query = _world.QueryBuilder.With<Camera>();
        foreach (var item in query)
        {
            ref var camera = ref item.Component1;
            ApplyMovementAndLook(ref camera, deltaTime);
        }
    }

    internal void ApplyMovementAndLook(ref Camera camera, float dt)
    {
        var axes = _input.GetMovementAxes();
        var look = _input.GetLookDelta();

        var forward = camera.Target - camera.Position;
        if (forward.LengthSquared() < 1e-6f)
            forward = new Vector3(0, 0, -1);
        forward = Vector3.Normalize(forward);

        var up = camera.Up;
        if (up.LengthSquared() < 1e-6f)
            up = Vector3.UnitY;
        up = Vector3.Normalize(up);

        var right = Vector3.Normalize(Vector3.Cross(forward, up));
        up = Vector3.Normalize(Vector3.Cross(right, forward));

        var move = (right * axes.X + up * axes.Y + forward * axes.Z) * MoveSpeed * dt;
        if (move.LengthSquared() > 0)
        {
            camera.Position += move;
            camera.Target += move;
        }

        var yaw = look.X * LookSpeed;
        var pitch = look.Y * LookSpeed;

        if (MathF.Abs(yaw) > 1e-7f || MathF.Abs(pitch) > 1e-7f)
        {
            var dir = forward;

            if (MathF.Abs(yaw) > 1e-7f)
            {
                var yawRot = Matrix4x4.CreateFromAxisAngle(up, yaw);
                dir = Vector3.Normalize(Vector3.TransformNormal(dir, yawRot));
            }

            right = Vector3.Normalize(Vector3.Cross(dir, up));

            if (MathF.Abs(pitch) > 1e-7f)
            {
                var pitchRot = Matrix4x4.CreateFromAxisAngle(right, pitch);
                dir = Vector3.Normalize(Vector3.TransformNormal(dir, pitchRot));
            }

            var distance = (camera.Target - camera.Position).Length();
            if (distance < 1e-6f)
                distance = 1f;

            camera.Target = camera.Position + dir * distance;
            camera.Up = Vector3.Normalize(Vector3.Cross(Vector3.Normalize(Vector3.Cross(dir, up)), dir));
        }
    }

    public void Dispose() { }
}