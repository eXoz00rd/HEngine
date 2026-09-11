using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using HEngine.Core.Components.Transform;
using HEngine.Core.Components.Rendering;
using HEngine.Core.Managers;

namespace HEngine.Core.Benchmarks.Queries;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class ComponentQueryBenchmarks
{
    private WorldManager _world = null!;

    [Params(100, 1000, 10000)]
    public int EntityCount;

    [GlobalSetup]
    public void Setup()
    {
        _world = new WorldManager(new SystemManager());

        for (int i = 0; i < EntityCount; i++)
        {
            var entity = _world.CreateEntity();
            _world.AddComponent(entity, new Transform());

            if (i % 2 == 0)
                _world.AddComponent(entity, new Camera());
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _world?.Dispose();
    }

    [Benchmark]
    public int QuerySingleComponent()
    {
        var query = _world.CreateQuery<Transform>();

        int count = 0;
        foreach (var (entity, transform) in query)
            count++;

        return count;
    }

    [Benchmark]
    public int QueryTwoComponents()
    {
        var query = _world.CreateQuery<Transform, Camera>();

        int count = 0;
        foreach (var (entity, transform, camera) in query)
            count++;

        return count;
    }

    [Benchmark]
    public int QueryWithModification()
    {
        var query = _world.CreateQuery<Transform>();

        int count = 0;
        foreach (var (entity, transform) in query)
        {
            ref var t = ref _world.GetComponent<Transform>(entity);
            t.IsDirty = true;
            count++;
        }

        return count;
    }

    [Benchmark]
    public int QueryCachedReuse()
    {
        var query = _world.CreateQuery<Transform>();

        int totalCount = 0;
        for (int iteration = 0; iteration < 10; iteration++)
        {
            int count = 0;
            foreach (var (entity, transform) in query)
                count++;
            totalCount += count;
        }

        return totalCount;
    }
}
