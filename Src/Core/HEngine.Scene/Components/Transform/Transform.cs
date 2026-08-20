using HEngine.Core.Contracts;
using HEngine.Core.Managers;
using HEngine.Core.Primitives;
using System.Numerics;

namespace HEngine.Core.Components.Transform;

public struct Transform : IComponent {
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Scale;

    public Entity Parent;
    public bool IsDirty;

    private Matrix4x4 _worldMatrix;
    private bool _worldMatrixCached;

    public Transform()
    {
        Position = Vector3.Zero;
        Rotation = Quaternion.Identity;
        Scale = Vector3.One;
        Parent = Entity.Null;
        IsDirty = false;
        _worldMatrix = Matrix4x4.Identity;
        _worldMatrixCached = false;
    }

    public Transform(Vector3 position, Quaternion rotation = default, Vector3 scale = default)
    {
        Position = position;
        Rotation = rotation == default ?
            Quaternion.Identity :
            rotation;
        if (scale == default)
            Scale = Vector3.One;
        else if (scale is { X: 0 } or { Y: 0 } or { Z: 0 })
            throw new ArgumentException("Scale nie może zawierać wartości zero");
        else
            Scale = scale;

        Parent = Entity.Null;
        IsDirty = true;
        _worldMatrix = Matrix4x4.Identity;
        _worldMatrixCached = false;
    }
    
    public Matrix4x4 ToMatrix()
        => Matrix4x4.CreateScale(Scale) *
            Matrix4x4.CreateFromQuaternion(Rotation) *
            Matrix4x4.CreateTranslation(Position);

    public Vector3 TransformPoint(Vector3 point)
        => Vector3.Transform(point, ToMatrix());

    public Vector3 TransformDirection(Vector3 direction)
        => Vector3.Transform(direction, Rotation);

    public Matrix4x4 GetWorldMatrix(WorldManager world)
    {
        if (_worldMatrixCached && !IsDirty)
            return _worldMatrix;

        var local = ToMatrix();

        if (Parent != Entity.Null && world.HasComponent<Transform>(Parent))
        {
            const int maxDepth = 64;
            int depth = 0;
            var current = Parent;
            var worldMatrix = local;

            while (current != Entity.Null && world.HasComponent<Transform>(current))
            {
                if (depth++ > maxDepth)
                    break;

                var parentTransform = world.GetComponent<Transform>(current);
                var parentLocal = parentTransform.ToMatrix();
                worldMatrix = parentLocal * worldMatrix;

                current = parentTransform.Parent;
            }

            _worldMatrix = worldMatrix;
        }
        else
        {
            _worldMatrix = local;
        }

        _worldMatrixCached = true;
        IsDirty = false;
        return _worldMatrix;
    }
}