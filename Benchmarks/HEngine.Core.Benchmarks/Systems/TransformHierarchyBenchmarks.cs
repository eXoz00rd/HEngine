using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using HEngine.Core.Components.Transform;
using HEngine.Core.Managers;
using HEngine.Core.Primitives;
using HEngine.Core.Systems;
using System.Numerics;

namespace HEngine.Core.Benchmarks.Systems;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class TransformHierarchyBenchmarks
{
    private WorldManager _world = null!;
    private TransformHierarchySystem _hierarchySystem = null!;
    private Entity[] _entities = null!;

    [Params(10, 100, 1000)]
    public int EntityCount;

    [GlobalSetup]
    public void Setup()
    {
        _world = new WorldManager();
        _hierarchySystem = new TransformHierarchySystem();
        _hierarchySystem.Initialize(_world);

        _entities = new Entity[EntityCount];

        Entity rootEntity = Entity.Null;
        for (int i = 0; i < EntityCount; i++)
        {
            var entity = _world.CreateEntity();
            _entities[i] = entity;

            var transform = new Transform(
                new Vector3(i, 0, 0),
                Quaternion.Identity,
                Vector3.One
            );

            if (i > 0 && i % 10 == 0)
            {
                rootEntity = entity;
                transform.Parent = Entity.Null;
            }
            else if (rootEntity != Entity.Null)
            {
                transform.Parent = rootEntity;
            }

            _world.AddComponent(entity, transform);
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _hierarchySystem?.Dispose();
        _world?.Dispose();
    }

    [Benchmark]
    public void UpdateHierarchySystem()
    {
        _hierarchySystem.Update(0.016f);
    }

    [Benchmark]
    public void GetWorldMatrixForAllEntities()
    {
        foreach (var entity in _entities)
        {
            if (_world.HasComponent<Transform>(entity))
            {
                ref var transform = ref _world.GetComponent<Transform>(entity);
                var worldMatrix = transform.GetWorldMatrix(_world);
            }
        }
    }

    [Benchmark]
    public void ModifyRootTransforms()
    {
        for (int i = 0; i < EntityCount; i += 10)
        {
            if (_world.HasComponent<Transform>(_entities[i]))
            {
                ref var transform = ref _world.GetComponent<Transform>(_entities[i]);
                transform.Position += new Vector3(1, 0, 0);
                transform.IsDirty = true;
            }
        }
    }

    [Benchmark]
    public void SetParentRelationships()
    {
        if (EntityCount < 2) return;

        var parent = _entities[0];
        for (int i = 1; i < Math.Min(EntityCount, 100); i++)
        {
            _hierarchySystem.SetParent(_entities[i], parent);
        }
    }

    [Benchmark]
    public void CalculateWorldMatricesWithDirtyFlags()
    {
        for (int i = 0; i < EntityCount; i += 5)
        {
            if (_world.HasComponent<Transform>(_entities[i]))
            {
                ref var transform = ref _world.GetComponent<Transform>(_entities[i]);
                transform.IsDirty = true;
            }
        }

        foreach (var entity in _entities)
        {
            if (_world.HasComponent<Transform>(entity))
            {
                ref var transform = ref _world.GetComponent<Transform>(entity);
                var worldMatrix = transform.GetWorldMatrix(_world);
            }
        }
    }
}
