using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using HEngine.Core.Components.Core;
using HEngine.Core.Components.Transform;
using HEngine.Core.Managers;
using HEngine.Core.Primitives;
using HEngine.Core.Systems;

namespace HEngine.Core.Scene;

public sealed class SceneGraph
{
    private readonly WorldManager _world;
    private readonly TransformHierarchySystem _hierarchy;

    public SceneGraph(WorldManager world)
    {
        _world = world ?? throw new ArgumentNullException(nameof(world));

        var existing = _world.GetSystem<TransformHierarchySystem>();
        if (existing is null)
        {
            _hierarchy = new TransformHierarchySystem();
            _world.AddSystem(_hierarchy);
        }
        else
        {
            _hierarchy = existing;
        }
    }

    public Entity CreateEntity(string? name = null, Entity parent = default)
    {
        var e = _world.CreateEntity();
        _world.AddComponent(e, new Transform());
        if (!string.IsNullOrWhiteSpace(name))
            _world.AddComponent(e, new Name(name!));

        if (parent != Entity.Null)
            SetParent(e, parent);

        return e;
    }

    public Entity SetParent(Entity child, Entity parent)
    {
        _hierarchy.SetParent(child, parent);
        return child;
    }

    public IEnumerable<Entity> GetChildren(Entity parent)
    {
        if (!_world.HasComponent<Children>(parent))
            return Enumerable.Empty<Entity>();

        var ch = _world.GetComponent<Children>(parent);
        var list = new List<Entity>(ch.Count);
        for (int i = 0; i < ch.Count; i++)
        {
            var c = ch.GetChild(i);
            if (c != Entity.Null)
                list.Add(c);
        }
        return list;
    }
    
    public void RemoveEntity(Entity entity)
    {
        if (_world.HasComponent<Children>(entity))
        {
            var ch = _world.GetComponent<Children>(entity);
            for (int i = 0; i < ch.Count; i++)
            {
                var c = ch.GetChild(i);
                if (c != Entity.Null)
                    RemoveEntity(c);
            }
        }

        _world.DestroyEntity(entity);
    }
}