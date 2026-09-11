using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using HEngine.Core.Contracts;
using HEngine.Core.Primitives;
using HEngine.Core.Storages;
using System.Collections.Concurrent;

namespace HEngine.Core.Benchmarks.Storages;

public struct Transform : IComponent {
    public float X, Y, Z;
    public float RotX, RotY, RotZ, RotW;
    public float ScaleX, ScaleY, ScaleZ;
}

public class ConcurrentDictionaryStorage<T> where T : struct, IComponent {
    public readonly ConcurrentDictionary<uint, T> Components = new();

    public int Count => Components.Count;

    public void AddComponent(Entity entity, T component)
        => Components[entity.Id] = component;

    public bool RemoveComponent(Entity entity)
        => Components.TryRemove(entity.Id, out _);

    public bool HasComponent(Entity entity)
        => Components.ContainsKey(entity.Id);

    public T GetComponent(Entity entity)
        => Components[entity.Id];

    public bool TryGetComponent(Entity entity, out T component)
        => Components.TryGetValue(entity.Id, out component);
}

[Config(typeof(BenchmarkConfig))]
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class ComponentStorageBenchmark {
    private ComponentStorage<Transform> _componentStorage = null!;
    private ConcurrentDictionaryStorage<Transform> _dictStorage = null!;
    private Entity[] _entities = null!;
    private Random _random = null!;
    private Transform[] _transforms = null!;

    [Params(100, 1_000, 10_000)] public int EntityCount;

    [GlobalSetup]
    public void Setup()
    {
        Console.WriteLine($"🔧 GlobalSetup rozpoczęty dla EntityCount = {EntityCount}");

        try
        {
            Console.WriteLine("   📦 Tworzenie ComponentStorage...");
            _componentStorage = new ComponentStorage<Transform>(EntityCount);

            Console.WriteLine("   📦 Tworzenie ConcurrentDictionaryStorage...");
            _dictStorage = new ConcurrentDictionaryStorage<Transform>();

            Console.WriteLine("   📦 Alokacja tablic...");
            _entities = new Entity[EntityCount];
            _transforms = new Transform[EntityCount];
            _random = new Random(42);

            Console.WriteLine("   🎲 Generowanie encji i komponentów...");
            for (var i = 0; i < EntityCount; i++)
            {
                _entities[i] = new Entity((uint)(i + 1));
                _transforms[i] = new Transform
                {
                    X = _random.NextSingle() * 100,
                    Y = _random.NextSingle() * 100,
                    Z = _random.NextSingle() * 100,
                    RotX = _random.NextSingle(),
                    RotY = _random.NextSingle(),
                    RotZ = _random.NextSingle(),
                    RotW = _random.NextSingle(),
                    ScaleX = 1.0f,
                    ScaleY = 1.0f,
                    ScaleZ = 1.0f
                };
                
                if (i > 0 && i % 1000 == 0)
                    Console.WriteLine($"   ⏳ Wygenerowano {i}/{EntityCount} elementów...");
            }

            Console.WriteLine("   💾 Dodawanie komponentów do storage'ów...");
            for (var i = 0; i < EntityCount; i++)
            {
                _componentStorage.AddComponent(_entities[i], _transforms[i]);
                _dictStorage.AddComponent(_entities[i], _transforms[i]);
                
                if (i > 0 && i % 1000 == 0)
                    Console.WriteLine($"   ⏳ Dodano {i}/{EntityCount} komponentów...");
            }

            Console.WriteLine($"✅ GlobalSetup zakończony pomyślnie dla EntityCount = {EntityCount}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ BŁĄD w GlobalSetup: {ex.Message}");
            Console.WriteLine($"   Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        Console.WriteLine("🧹 GlobalCleanup rozpoczęty...");
        try
        {
            _componentStorage?.Dispose();
            Console.WriteLine("✅ GlobalCleanup zakończony pomyślnie");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ BŁĄD w GlobalCleanup: {ex.Message}");
            throw;
        }
    }


    [Benchmark]
    public Transform GetComponent_ComponentStorage()
    {
        var entity = _entities[_random.Next(EntityCount)];
        return _componentStorage.GetComponent(entity);
    }

    [Benchmark]
    public Transform GetComponent_ConcurrentDict()
    {
        var entity = _entities[_random.Next(EntityCount)];
        return _dictStorage.GetComponent(entity);
    }

    [Benchmark]
    public bool HasComponent_ComponentStorage()
    {
        var entity = _entities[_random.Next(EntityCount)];
        return _componentStorage.HasComponent(entity);
    }

    [Benchmark]
    public bool HasComponent_ConcurrentDict()
    {
        var entity = _entities[_random.Next(EntityCount)];
        return _dictStorage.HasComponent(entity);
    }

    [Benchmark]
    public bool TryGetComponent_ComponentStorage()
    {
        var entity = _entities[_random.Next(EntityCount)];
        return _componentStorage.TryGetComponent(entity, out var component);
    }

    [Benchmark]
    public bool TryGetComponent_ConcurrentDict()
    {
        var entity = _entities[_random.Next(EntityCount)];
        return _dictStorage.TryGetComponent(entity, out var component);
    }


    [Benchmark]
    public int GetAllComponents_ComponentStorage()
    {
        var components = _componentStorage.GetAllComponents();
        return components.Length;
    }

    [Benchmark]
    public int GetAllComponentsReadOnly_ComponentStorage()
    {
        var components = _componentStorage.GetAllComponentsReadOnly();
        return components.Length;
    }


    [Benchmark]
    public float IterateAllComponents_ComponentStorage()
    {
        var components = _componentStorage.GetAllComponentsReadOnly();
        float sum = 0;
        foreach (ref readonly var component in components)
            sum += component.X + component.Y + component.Z;

        return sum;
    }

    [Benchmark]
    public float IterateAllComponents_ConcurrentDict()
    {
        float sum = 0;
        foreach (var kvp in _dictStorage.Components)
        {
            var component = kvp.Value;
            sum += component.X + component.Y + component.Z;
        }

        return sum;
    }


    [Benchmark]
    public void AddRemove_ComponentStorage()
    {
        using var storage = new ComponentStorage<Transform>();

        for (var i = 0; i < EntityCount / 10; i++)
            storage.AddComponent(_entities[i], _transforms[i]);

        for (var i = 0; i < EntityCount / 10; i++)
            storage.RemoveComponent(_entities[i]);
    }

    [Benchmark]
    public void AddRemove_ConcurrentDict()
    {
        var storage = new ConcurrentDictionaryStorage<Transform>();

        for (var i = 0; i < EntityCount / 10; i++)
            storage.AddComponent(_entities[i], _transforms[i]);

        for (var i = 0; i < EntityCount / 10; i++)
            storage.RemoveComponent(_entities[i]);
    }
    
    [Benchmark]
    public int MixedWorkload_ComponentStorage()
    {
        using var storage = new ComponentStorage<Transform>();
        var operations = 0;

        var workloadSize = Math.Min(EntityCount / 10, 1000);

        for (var i = 0; i < workloadSize; i++)
        {
            var entity = _entities[i];
            var transform = _transforms[i];
            
            storage.AddComponent(entity, transform);
            operations++;

            for (var j = 0; j < 3; j++)
            {
                _ = storage.GetComponent(entity);
                operations++;
            }

            if (storage.HasComponent(entity))
            {
                _ = storage.TryGetComponent(entity, out _);
                operations += 2;
            }

            storage.RemoveComponent(entity);
            operations++;
        }

        return operations;
    }

    [Benchmark]
    public int MixedWorkload_ConcurrentDict()
    {
        var storage = new ConcurrentDictionaryStorage<Transform>();
        var operations = 0;

        var workloadSize = Math.Min(EntityCount / 10, 1000);

        for (var i = 0; i < workloadSize; i++)
        {
            var entity = _entities[i];
            var transform = _transforms[i];

            storage.AddComponent(entity, transform);
            operations++;

            for (var j = 0; j < 3; j++)
            {
                _ = storage.GetComponent(entity);
                operations++;
            }

            if (storage.HasComponent(entity))
            {
                _ = storage.TryGetComponent(entity, out _);
                operations += 2;
            }

            storage.RemoveComponent(entity);
            operations++;
        }

        return operations;
    }

    [Benchmark]
    public long GetMemoryUsage_ComponentStorage()
        => _componentStorage.GetMemoryUsage();

    [Benchmark]
    public ComponentStorageStats GetStats_ComponentStorage()
        => _componentStorage.GetStats();
}

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class ComponentStorageMemoryBenchmark {
    [Params(1000, 10000)] public int EntityCount;

    [GlobalSetup]
    public void Setup()
        => Console.WriteLine($"🔧 Memory Benchmark GlobalSetup dla EntityCount = {EntityCount}");

    [Benchmark]
    public void AllocateAndDispose_ComponentStorage()
    {
        using var storage = new ComponentStorage<Transform>();

        for (var i = 0; i < EntityCount; i++)
        {
            var entity = new Entity((uint)(i + 1));
            storage.AddComponent(entity, new Transform { X = i, Y = i, Z = i });
        }
    }

    [Benchmark]
    public void AllocateAndDispose_ConcurrentDict()
    {
        var storage = new ConcurrentDictionaryStorage<Transform>();

        for (var i = 0; i < EntityCount; i++)
        {
            var entity = new Entity((uint)(i + 1));
            storage.AddComponent(entity, new Transform { X = i, Y = i, Z = i });
        }
    }
}

public class BenchmarkConfig : ManualConfig {
    public BenchmarkConfig()
    {
        Console.WriteLine("🔧 Konfiguracja BenchmarkConfig...");
        AddLogger(ConsoleLogger.Default);

        AddJob(
            Job.Default
               .WithRuntime(CoreRuntime.Core90)
               .WithPlatform(Platform.X64)
               .WithJit(Jit.RyuJit)
               .WithGcServer(true)
        );

        WithOptions(ConfigOptions.DisableOptimizationsValidator);

        Console.WriteLine("✅ Konfiguracja BenchmarkConfig ukończona");
    }
}

public class Program {
    public static void Main(string[] args)
    {
        Console.WriteLine("🚀 HEngine Benchmark Suite");
        Console.WriteLine("==========================");
        Console.WriteLine($"📅 Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"🖥️  Platform: {Environment.OSVersion}");
        Console.WriteLine($"🔧 .NET Version: {Environment.Version}");
        Console.WriteLine($"⚙️  Processor Count: {Environment.ProcessorCount}");
        Console.WriteLine();

        try
        {
            Console.WriteLine("🔧 Creating benchmark configuration...");
            var config = new BenchmarkConfig();

            Console.WriteLine("🎯 Running ComponentStorageBenchmark...");
            var summary1 = BenchmarkRunner.Run<ComponentStorageBenchmark>(config);
            Console.WriteLine($"✅ Storage benchmarks completed. Reports: {summary1?.Reports.Length ?? 0}");

            Console.WriteLine("🎯 Running ComponentStorageMemoryBenchmark...");
            var summary2 = BenchmarkRunner.Run<ComponentStorageMemoryBenchmark>(config);
            Console.WriteLine($"✅ Memory benchmarks completed. Reports: {summary2?.Reports.Length ?? 0}");

            Console.WriteLine("🎯 Running ComponentQueryBenchmarks...");
            var summary3 = BenchmarkRunner.Run<HEngine.Core.Benchmarks.Queries.ComponentQueryBenchmarks>(config);
            Console.WriteLine($"✅ Query benchmarks completed. Reports: {summary3?.Reports.Length ?? 0}");

            Console.WriteLine("🎯 Running TransformHierarchyBenchmarks...");
            var summary4 = BenchmarkRunner.Run<HEngine.Core.Benchmarks.Systems.TransformHierarchyBenchmarks>(config);
            Console.WriteLine($"✅ Hierarchy benchmarks completed. Reports: {summary4?.Reports.Length ?? 0}");

            Console.WriteLine("\n🎉 All benchmarks completed successfully!");
            if (summary1?.ResultsDirectoryPath != null)
                Console.WriteLine($"📁 Results saved to: {summary1.ResultsDirectoryPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine("\n❌ CRITICAL ERROR during benchmark execution:");
            Console.WriteLine($"   Type: {ex.GetType().Name}");
            Console.WriteLine($"   Message: {ex.Message}");
            Console.WriteLine($"   Stack trace:\n{ex.StackTrace}");

            if (ex.InnerException != null)
            {
                Console.WriteLine($"\n   Inner Exception: {ex.InnerException.GetType().Name}");
                Console.WriteLine($"   Inner Message: {ex.InnerException.Message}");
            }

            Console.WriteLine("\n❌ Application will exit with error code.");
            Environment.Exit(1);
        }

        Console.WriteLine("\n⏳ Press any key to exit...");
        Console.ReadKey();
    }
}