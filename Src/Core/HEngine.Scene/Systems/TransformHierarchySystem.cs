using HEngine.Core.Components.Core;
using HEngine.Core.Components.Transform;
using HEngine.Core.Contracts;
using HEngine.Core.Managers;
using HEngine.Core.Primitives;

namespace HEngine.Core.Systems;

public sealed class TransformHierarchySystem : ISystem
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

        var query = _world.QueryBuilder.With<Transform>();

        foreach (var item in query)
        {
            var entity = item.Entity;
            ref var transform = ref item.Component1;
            if (transform.IsDirty)
            {
                MarkHierarchyDirty(entity);
            }
        }

        foreach (var item in query)
        {
            ref var transform = ref item.Component1;
            if (transform.IsDirty)
            {
                transform.GetWorldMatrix(_world);
            }
        }
    }

    public void SetParent(Entity child, Entity parent)
    {
        if (_world is null)
            throw new InvalidOperationException("System not initialized");

        if (!_world.HasComponent<Transform>(child))
            throw new ArgumentException("Child entity must have a Transform component", nameof(child));

        ref var childTransform = ref _world.GetComponent<Transform>(child);
        var previousParent = childTransform.Parent;

        if (parent == Entity.Null)
        {
            if (previousParent != Entity.Null && _world.HasComponent<Children>(previousParent))
            {
                ref var prevChildren = ref _world.GetComponent<Children>(previousParent);
                prevChildren.Remove(child);
            }

            childTransform.Parent = Entity.Null;
            childTransform.IsDirty = true;
            MarkHierarchyDirty(child);
            return;
        }

        if (child == parent)
            throw new InvalidOperationException("Cannot set an entity as its own parent");
        
        var current = parent;
        const int maxDepth = 1024;
        int depth = 0;
        while (current != Entity.Null && _world.HasComponent<Transform>(current))
        {
            if (++depth > maxDepth)
                throw new InvalidOperationException("Hierarchy too deep or contains a cycle");

            if (current == child)
                throw new InvalidOperationException("Setting this parent would create a circular reference");

            current = _world.GetComponent<Transform>(current).Parent;
        }
        
        if (previousParent != Entity.Null && _world.HasComponent<Children>(previousParent))
        {
            ref var prevChildren = ref _world.GetComponent<Children>(previousParent);
            prevChildren.Remove(child);
        }
        
        if (_world.HasComponent<Children>(parent))
        {
            ref var ch = ref _world.GetComponent<Children>(parent);
            ch.Add(child);
        }
        else
        {
            var ch = new Children();
            ch.Add(child);
            _world.AddComponent(parent, ch);
        }
        
        childTransform.Parent = parent;
        childTransform.IsDirty = true;
        MarkHierarchyDirty(child);
    }

    private void MarkHierarchyDirty(Entity entity)
    {
        if (_world is null)
            return;
        
        if (_world.HasComponent<Transform>(entity))
        {
            ref var t = ref _world.GetComponent<Transform>(entity);
            t.IsDirty = true;
        }
        
        if (_world.HasComponent<Children>(entity))
        {
            ref var children = ref _world.GetComponent<Children>(entity);
            for (int i = 0; i < children.Count; i++)
            {
                var child = children.GetChild(i);
                if (child != Entity.Null)
                {
                    if (_world.HasComponent<Transform>(child))
                    {
                        ref var ct = ref _world.GetComponent<Transform>(child);
                        ct.IsDirty = true;
                    }
                    MarkHierarchyDirty(child);
                }
            }
        }
    }

    public void Dispose() { }
}
