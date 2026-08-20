using System;
using System.Numerics;
using HEngine.Core.Components.Rendering;
using HEngine.Core.Components.Transform;
using HEngine.Core.Contracts;
using HEngine.Core.Managers;
using HEngine.Core.Mathematics;
using HEngine.Core.Primitives;

namespace HEngine.Core.Systems;

public sealed class FrustumCullingSystem : ISystem
{
    private WorldManager? _world;

    public void Initialize(WorldManager worldManager)
    {
        _world = worldManager ?? throw new ArgumentNullException(nameof(worldManager));
    }

    public void Update(float deltaTime)
    {
        if (_world is null)
            return;

        var camQuery = _world.CreateQuery<Camera>();
        if (!camQuery.TryGetFirst(out var camEntity, out var camera))
            return;

        var view = camera.GetViewMatrix();
        var proj = camera.GetProjectionMatrix();
        var viewProj = view * proj;
        var frustum = Frustum.FromViewProjection(viewProj);
        
        var query = _world.CreateQuery<Transform, BoundingBox>();
        foreach (var item in query)
        {
            ref var transform = ref item.Component1;
            ref var bounds = ref item.Component2;

            var worldMatrix = transform.GetWorldMatrix(_world);
            var worldAabb = TransformAabb(bounds, worldMatrix);

            bool inside = frustum.Intersects(worldAabb);
            bool hasCulled = _world.HasComponent<Culled>(item.Entity);

            if (!inside && !hasCulled)
            {
                _world.AddComponent(item.Entity, new Culled());
            }
            else if (inside && hasCulled)
            {
                _world.RemoveComponent<Culled>(item.Entity);
            }
        }
    }

    public void Dispose()
    {
    }

    private static Aabb TransformAabb(in BoundingBox localBox, in Matrix4x4 world)
    {
       var center = localBox.Center;
        var ext = localBox.Extents;

        var worldCenter = Vector3.Transform(center, world);

        var m11 = MathF.Abs(world.M11); var m12 = MathF.Abs(world.M12); var m13 = MathF.Abs(world.M13);
        var m21 = MathF.Abs(world.M21); var m22 = MathF.Abs(world.M22); var m23 = MathF.Abs(world.M23);
        var m31 = MathF.Abs(world.M31); var m32 = MathF.Abs(world.M32); var m33 = MathF.Abs(world.M33);

        var worldExtents = new Vector3(
            m11 * ext.X + m12 * ext.Y + m13 * ext.Z,
            m21 * ext.X + m22 * ext.Y + m23 * ext.Z,
            m31 * ext.X + m32 * ext.Y + m33 * ext.Z);

        return Aabb.FromCenterExtents(worldCenter, worldExtents);
    }
}
