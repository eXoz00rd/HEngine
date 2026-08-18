using HEngine.Core.Contracts;
using HEngine.Core.Managers;
using HEngine.Core.Primitives;
using NSubstitute;

namespace HEngine.Core.Tests.Managers;

public class WorldManagerTests : IDisposable {
    private readonly WorldManager _worldManager = new(new SystemManager());

    public void Dispose()
        => _worldManager.Dispose();

    [Fact]
    public void Constructor_ShouldInitializeAllManagers()
    {
        using var worldManager = new WorldManager(new SystemManager());

        Assert.NotNull(worldManager.EntityManager);
        Assert.NotNull(worldManager.ComponentManager);
        Assert.NotNull(worldManager.QueryBuilder);
    }

    [Fact]
    public void CreateEntity_ShouldReturnValidEntity()
    {
        using var worldManager = new WorldManager(new SystemManager());
        var entity = worldManager.CreateEntity();

        Assert.NotEqual(Entity.Null, entity);
        Assert.Equal(1, worldManager.GetEntityCount());
    }

    [Fact]
    public void CreateEntity_Multiple_ShouldReturnUniqueEntities()
    {
        using var worldManager = new WorldManager(new SystemManager());
        var entity1 = worldManager.CreateEntity();
        var entity2 = worldManager.CreateEntity();
        var entity3 = worldManager.CreateEntity();

        Assert.NotEqual(entity1, entity2);
        Assert.NotEqual(entity2, entity3);
        Assert.NotEqual(entity1, entity3);
        Assert.Equal(3, worldManager.GetEntityCount());
    }

    [Fact]
    public void DestroyEntity_WithValidEntity_ShouldRemoveEntity()
    {
        using var worldManager = new WorldManager(new SystemManager());
        var entity = worldManager.CreateEntity();
        worldManager.AddComponent(entity, new TestComponent { Value = 42 });

        worldManager.DestroyEntity(entity);

        Assert.Equal(0, worldManager.GetEntityCount());
        Assert.False(worldManager.HasComponent<TestComponent>(entity));
    }

    [Fact]
    public void DestroyEntity_WithInvalidEntity_ShouldNotThrow()
    {
        var exception = Record.Exception(RemoveSystem);

        Assert.Null(exception);
        return;

        void RemoveSystem()
        {
            using var worldManager = new WorldManager(new SystemManager());
            var invalidEntity = new Entity(999);
            worldManager.DestroyEntity(invalidEntity);
        }
    }

    [Fact]
    public void AddComponent_WithValidEntity_ShouldAddComponent()
    {
        using var worldManager = new WorldManager(new SystemManager());
        var entity = worldManager.CreateEntity();
        var component = new TestComponent { Value = 42 };

        ref var result = ref worldManager.AddComponent(entity, in component);

        Assert.Equal(42, result.Value);
        Assert.True(worldManager.HasComponent<TestComponent>(entity));
    }

    [Fact]
    public void AddComponent_WithInvalidEntity_ShouldThrowArgumentException()
    {
        using var worldManager = new WorldManager(new SystemManager());
        var invalidEntity = new Entity(999);
        var component = new TestComponent { Value = 42 };

        Assert.Throws<ArgumentException>(() => worldManager.AddComponent(invalidEntity, in component));
    }

    [Fact]
    public void RemoveComponent_WithValidEntity_ShouldRemoveComponent()
    {
        using var worldManager = new WorldManager(new SystemManager());
        var entity = worldManager.CreateEntity();
        worldManager.AddComponent(entity, new TestComponent { Value = 42 });

        var result = worldManager.RemoveComponent<TestComponent>(entity);

        Assert.True(result);
        Assert.False(worldManager.HasComponent<TestComponent>(entity));
    }

    [Fact]
    public void RemoveComponent_WithInvalidEntity_ShouldReturnFalse()
    {
        using var worldManager = new WorldManager(new SystemManager());
        var invalidEntity = new Entity(999);

        var result = worldManager.RemoveComponent<TestComponent>(invalidEntity);

        Assert.False(result);
    }

    [Fact]
    public void RemoveComponent_WithValidEntityButNoComponent_ShouldReturnFalse()
    {
        using var worldManager = new WorldManager(new SystemManager());
        var entity = worldManager.CreateEntity();

        var result = worldManager.RemoveComponent<TestComponent>(entity);

        Assert.False(result);
    }

    [Fact]
    public void GetComponent_WithValidEntity_ShouldReturnComponent()
    {
        using var worldManager = new WorldManager(new SystemManager());
        var entity = worldManager.CreateEntity();
        worldManager.AddComponent(entity, new TestComponent { Value = 42 });

        ref var component = ref worldManager.GetComponent<TestComponent>(entity);

        Assert.Equal(42, component.Value);
    }

    [Fact]
    public void GetComponent_WithInvalidEntity_ShouldThrowArgumentException()
    {
        using var worldManager = new WorldManager(new SystemManager());
        var invalidEntity = new Entity(999);

        Assert.Throws<ArgumentException>(() => worldManager.GetComponent<TestComponent>(invalidEntity));
    }

    [Fact]
    public void GetComponent_ShouldReturnReference()
    {
        using var worldManager = new WorldManager(new SystemManager());
        var entity = worldManager.CreateEntity();
        worldManager.AddComponent(entity, new TestComponent { Value = 42 });

        ref var component = ref worldManager.GetComponent<TestComponent>(entity);
        component.Value = 100;

        ref var componentAgain = ref worldManager.GetComponent<TestComponent>(entity);
        Assert.Equal(100, componentAgain.Value);
    }

    [Fact]
    public void HasComponent_WithValidEntity_ShouldReturnCorrectValue()
    {
        using var worldManager = new WorldManager(new SystemManager());
        var entity = worldManager.CreateEntity();

        Assert.False(worldManager.HasComponent<TestComponent>(entity));

        worldManager.AddComponent(entity, new TestComponent { Value = 42 });

        Assert.True(worldManager.HasComponent<TestComponent>(entity));
    }

    [Fact]
    public void HasComponent_WithInvalidEntity_ShouldReturnFalse()
    {
        using var worldManager = new WorldManager(new SystemManager());
        var invalidEntity = new Entity(999);

        Assert.False(worldManager.HasComponent<TestComponent>(invalidEntity));
    }

    [Fact]
    public void TryGetComponent_WithValidEntity_ShouldReturnComponent()
    {
        using var worldManager = new WorldManager(new SystemManager());
        var entity = worldManager.CreateEntity();
        worldManager.AddComponent(entity, new TestComponent { Value = 42 });

        var result = worldManager.TryGetComponent<TestComponent>(entity, out var component);

        Assert.True(result);
        Assert.Equal(42, component.Value);
    }

    [Fact]
    public void TryGetComponent_WithInvalidEntity_ShouldReturnFalse()
    {
        using var worldManager = new WorldManager(new SystemManager());
        var invalidEntity = new Entity(999);

        var result = worldManager.TryGetComponent<TestComponent>(invalidEntity, out var component);

        Assert.False(result);
        Assert.Equal(default, component);
    }

    [Fact]
    public void TryGetComponent_WithValidEntityButNoComponent_ShouldReturnFalse()
    {
        using var worldManager = new WorldManager(new SystemManager());
        var entity = worldManager.CreateEntity();

        var result = worldManager.TryGetComponent<TestComponent>(entity, out var component);

        Assert.False(result);
        Assert.Equal(default, component);
    }

    [Fact]
    public void AddSystem_WithNewSystem_ShouldAddSystem()
    {
        using var worldManager = new WorldManager(new SystemManager());
        var system = Substitute.For<ISystem>();

        worldManager.AddSystem(system, 1);

        Assert.True(worldManager.HasSystem<ISystem>());
        Assert.Equal(1, worldManager.GetSystemCount());
        system.Received(1).Initialize(worldManager);
    }

    [Fact]
    public void AddSystem_WithDefaultParameters_ShouldAddSystem()
    {
        using var worldManager = new WorldManager(new SystemManager());
        var system = Substitute.For<ISystem>();

        worldManager.AddSystem(system);

        Assert.True(worldManager.HasSystem<ISystem>());
        Assert.Equal(1, worldManager.GetSystemCount());
        system.Received(1).Initialize(worldManager);
    }

    [Fact]
    public void AddSystem_WithExistingSystem_ShouldThrowInvalidOperationException()
    {
        using var worldManager = new WorldManager(new SystemManager());
        var system1 = Substitute.For<ISystem>();
        var system2 = Substitute.For<ISystem>();

        worldManager.AddSystem(system1);

        Assert.Throws<InvalidOperationException>(() => worldManager.AddSystem(system2));
    }

    [Fact]
    public void RemoveSystem_WithExistingSystem_ShouldRemoveSystem()
    {
        using var worldManager = new WorldManager(new SystemManager());
        var system = Substitute.For<ISystem>();
        worldManager.AddSystem(system);

        worldManager.RemoveSystem<ISystem>();

        Assert.False(worldManager.HasSystem<ISystem>());
        Assert.Equal(0, worldManager.GetSystemCount());
    }

    [Fact]
    public void RemoveSystem_WithNonExistingSystem_ShouldNotThrow()
    {
        var exception = Record.Exception(Action);

        Assert.Null(exception);

        return;

        void Action()
        {
            using var worldManager = new WorldManager(new SystemManager());

            worldManager.RemoveSystem<ISystem>();
        }
    }

    [Fact]
    public void SetSystemEnabled_WithExistingSystem_ShouldToggleState()
    {
        using var worldManager = new WorldManager(new SystemManager());
        var system = Substitute.For<ISystem>();
        worldManager.AddSystem(system);

        worldManager.SetSystemEnabled<ISystem>(false);

        Assert.Equal(0, worldManager.GetActiveSystemCount());
        Assert.Equal(1, worldManager.GetSystemCount());
    }

    [Fact]
    public void SetSystemEnabled_WithNonExistingSystem_ShouldNotThrow()
    {
        var exception = Record.Exception(Action);

        Assert.Null(exception);

        return;

        void Action()
        {
            using var worldManager = new WorldManager(new SystemManager());
            worldManager.SetSystemEnabled<ISystem>(false);
        }
    }

    [Fact]
    public void GetSystem_WithExistingSystem_ShouldReturnSystem()
    {
        using var worldManager = new WorldManager(new SystemManager());
        var system = Substitute.For<TestSystem>();
        worldManager.AddSystem(system);

        var result = worldManager.GetSystem<TestSystem>();

        Assert.Same(system, result);
    }

    [Fact]
    public void GetSystem_WithNonExistingSystem_ShouldReturnNull()
    {
        using var worldManager = new WorldManager(new SystemManager());

        var result = worldManager.GetSystem<TestSystem>();

        Assert.Null(result);
    }

    [Fact]
    public void HasSystem_WithExistingSystem_ShouldReturnTrue()
    {
        using var worldManager = new WorldManager(new SystemManager());
        var system = Substitute.For<ISystem>();
        worldManager.AddSystem(system);

        Assert.True(worldManager.HasSystem<ISystem>());
    }

    [Fact]
    public void HasSystem_WithNonExistingSystem_ShouldReturnFalse()
    {
        using var worldManager = new WorldManager(new SystemManager());

        Assert.False(worldManager.HasSystem<ISystem>());
    }

    [Fact]
    public void CreateQuery_ShouldReturnQueryAndCacheIt()
    {
        using var worldManager = new WorldManager(new SystemManager());

        var query = worldManager.CreateQuery<TestComponent>();

        Assert.NotNull(query);
    }

    [Fact]
    public void CreateQuery_WithTwoComponents_ShouldReturnQueryAndCacheIt()
    {
        using var worldManager = new WorldManager(new SystemManager());

        var query = worldManager.CreateQuery<TestComponent, TestComponent2>();

        Assert.NotNull(query);
    }

    [Fact]
    public void CreateQuery_WithThreeComponents_ShouldReturnQueryAndCacheIt()
    {
        using var worldManager = new WorldManager(new SystemManager());

        var query = worldManager.CreateQuery<TestComponent, TestComponent2, TestComponent3>();

        Assert.NotNull(query);
    }

    [Fact]
    public void CreateQuery_CalledRepeatedlyForSameShape_ShouldReuseCachedInstance()
    {
        using var worldManager = new WorldManager(new SystemManager());

        var first = worldManager.CreateQuery<TestComponent>();
        for (var i = 0; i < 1000; i++)
        {
            var repeated = worldManager.CreateQuery<TestComponent>();
            Assert.Same(first, repeated);
        }
    }

    [Fact]
    public void CreateQuery_WithTwoComponents_CalledRepeatedly_ShouldReuseCachedInstance()
    {
        using var worldManager = new WorldManager(new SystemManager());

        var first = worldManager.CreateQuery<TestComponent, TestComponent2>();
        var repeated = worldManager.CreateQuery<TestComponent, TestComponent2>();

        Assert.Same(first, repeated);
    }

    [Fact]
    public void CreateQuery_WithThreeComponents_CalledRepeatedly_ShouldReuseCachedInstance()
    {
        using var worldManager = new WorldManager(new SystemManager());

        var first = worldManager.CreateQuery<TestComponent, TestComponent2, TestComponent3>();
        var repeated = worldManager.CreateQuery<TestComponent, TestComponent2, TestComponent3>();

        Assert.Same(first, repeated);
    }

    [Fact]
    public void CreateQuery_DifferentShapesSharingAComponent_ShouldNotReuseCachedInstance()
    {
        using var worldManager = new WorldManager(new SystemManager());

        var single = worldManager.CreateQuery<TestComponent>();
        var pair = worldManager.CreateQuery<TestComponent, TestComponent2>();

        Assert.NotSame((object)single, pair);
    }

    [Fact]
    public void CreateQuery_ReusedInstance_ShouldStillReceiveInvalidationUpdates()
    {
        using var worldManager = new WorldManager(new SystemManager());
        var entity = worldManager.CreateEntity();

        var first = worldManager.CreateQuery<TestComponent>();
        Assert.Equal(0, first.Count);

        var reused = worldManager.CreateQuery<TestComponent>();
        worldManager.AddComponent(entity, new TestComponent { Value = 1 });

        Assert.Same(first, reused);
        Assert.Equal(1, reused.Count);
    }

    [Fact]
    public void DestroyEntities_WithValidEntities_ShouldDestroyAll()
    {
        using var worldManager = new WorldManager(new SystemManager());
        var entities = new Entity[3];
        for (var i = 0; i < 3; i++)
        {
            entities[i] = worldManager.CreateEntity();
            worldManager.AddComponent(entities[i], new TestComponent { Value = i });
        }

        worldManager.DestroyEntities(entities);

        Assert.Equal(0, worldManager.GetEntityCount());
        Assert.Equal(0, worldManager.GetComponentCount<TestComponent>());
    }

    [Fact]
    public void DestroyEntities_WithMixedValidAndInvalidEntities_ShouldDestroyOnlyValid()
    {
        using var worldManager = new WorldManager(new SystemManager());
        var validEntity = worldManager.CreateEntity();
        var invalidEntity = new Entity(999);
        worldManager.AddComponent(validEntity, new TestComponent { Value = 42 });

        var entities = new[] { validEntity, invalidEntity };

        worldManager.DestroyEntities(entities);

        Assert.Equal(0, worldManager.GetEntityCount());
    }

    [Fact]
    public void DestroyEntities_WithEmptySpan_ShouldNotThrow()
    {
        var exception = Record.Exception(Action);

        Assert.Null(exception);

        return;

        void Action()
        {
            using var worldManager = new WorldManager(new SystemManager());
            worldManager.DestroyEntities(ReadOnlySpan<Entity>.Empty);
        }
    }

    [Fact]
    public void AddComponents_WithValidEntities_ShouldAddAllComponents()
    {
        using var worldManager = new WorldManager(new SystemManager());
        var entities = new Entity[3];
        for (var i = 0; i < 3; i++)
            entities[i] = worldManager.CreateEntity();

        var components = new (Entity, TestComponent)[3];
        for (var i = 0; i < 3; i++)
            components[i] = (entities[i], new TestComponent { Value = i });

        worldManager.AddComponents<TestComponent>(components);

        for (var i = 0; i < 3; i++)
        {
            Assert.True(worldManager.HasComponent<TestComponent>(entities[i]));
            Assert.Equal(i, worldManager.GetComponent<TestComponent>(entities[i]).Value);
        }
    }

    [Fact]
    public void AddComponents_WithInvalidEntities_ShouldSkipInvalidOnes()
    {
        using var worldManager = new WorldManager(new SystemManager());
        var validEntity = worldManager.CreateEntity();
        var invalidEntity = new Entity(999);

        var components = new[]
        {
            (validEntity, new TestComponent { Value = 1 }),
            (invalidEntity, new TestComponent { Value = 2 })
        };

        worldManager.AddComponents<TestComponent>(components);

        Assert.True(worldManager.HasComponent<TestComponent>(validEntity));
        Assert.Equal(1, worldManager.GetComponentCount<TestComponent>());
    }

    [Fact]
    public void AddComponents_WithEmptySpan_ShouldNotThrow()
    {
        var exception = Record.Exception(Action);
        Assert.Null(exception);

        return;

        void Action()
        {
            using var worldManager = new WorldManager(new SystemManager());
            worldManager.AddComponents(ReadOnlySpan<(Entity, TestComponent)>.Empty);
        }
    }

    [Fact]
    public void Update_ShouldCallSystemManagerUpdate()
    {
        using var worldManager = new WorldManager(new SystemManager());
        var system = Substitute.For<ISystem>();
        worldManager.AddSystem(system);

        worldManager.Update(0.16f);

        system.Received(1).Update(0.16f);
    }

    [Fact]
    public void Update_WithDisabledSystem_ShouldNotCallUpdate()
    {
        using var worldManager = new WorldManager(new SystemManager());
        var system = Substitute.For<ISystem>();
        worldManager.AddSystem(system);
        worldManager.SetSystemEnabled<ISystem>(false);

        worldManager.Update(0.16f);

        system.DidNotReceive().Update(Arg.Any<float>());
    }

    [Fact]
    public void Update_WithNoSystems_ShouldNotThrow()
    {
        var exception = Record.Exception(Action);

        Assert.Null(exception);

        return;

        void Action()
        {
            using var worldManager = new WorldManager(new SystemManager());
            worldManager.Update(0.16f);
        }
    }

    [Fact]
    public void AddSystem_ThenUpdateOnSharedSystemManager_ShouldRunTheSystem()
    {
        var systemManager = new SystemManager();
        using var worldManager = new WorldManager(systemManager);
        var system = Substitute.For<ISystem>();

        worldManager.AddSystem(system);
        systemManager.Update(0.16f);

        system.Received(1).Update(0.16f);
    }

    [Fact]
    public void Dispose_ShouldCleanupResourcesAndPreventFurtherOperations()
    {
        var worldManager = new WorldManager(new SystemManager());
        var entity = worldManager.CreateEntity();
        worldManager.AddComponent(entity, new TestComponent { Value = 42 });

        worldManager.Dispose();

        worldManager.Dispose();

        Assert.Throws<ObjectDisposedException>(() => worldManager.GetEntityCount());
    }

    [Fact]
    public void Dispose_ShouldNotDisposeTheSharedSystemManager()
    {
        var systemManager = new SystemManager();
        var worldManager = new WorldManager(systemManager);
        var system = Substitute.For<ISystem>();
        worldManager.AddSystem(system);

        worldManager.Dispose();
        systemManager.Update(0.16f);

        system.Received(1).Update(0.16f);
    }

    [Fact]
    public void GetEntityCount_ShouldReturnCorrectCount()
    {
        using var worldManager = new WorldManager(new SystemManager());

        Assert.Equal(0, worldManager.GetEntityCount());

        var entity1 = worldManager.CreateEntity();
        Assert.Equal(1, worldManager.GetEntityCount());

        worldManager.CreateEntity();
        Assert.Equal(2, worldManager.GetEntityCount());

        worldManager.DestroyEntity(entity1);
        Assert.Equal(1, worldManager.GetEntityCount());
    }

    [Fact]
    public void GetSystemCount_ShouldReturnCorrectCount()
    {
        using var worldManager = new WorldManager(new SystemManager());

        Assert.Equal(0, worldManager.GetSystemCount());

        var system1 = Substitute.For<ISystem>();
        worldManager.AddSystem(system1);
        Assert.Equal(1, worldManager.GetSystemCount());

        var system2 = Substitute.For<TestSystem>();
        worldManager.AddSystem(system2);
        Assert.Equal(2, worldManager.GetSystemCount());

        worldManager.RemoveSystem<ISystem>();
        Assert.Equal(1, worldManager.GetSystemCount());
    }

    [Fact]
    public void GetActiveSystemCount_WithEnabledSystems_ShouldReturnCorrectCount()
    {
        using var worldManager = new WorldManager(new SystemManager());

        Assert.Equal(0, worldManager.GetActiveSystemCount());

        var system1 = new TestSystemA();
        worldManager.AddSystem(system1);
        worldManager.Update(0f);
        Assert.Equal(1, worldManager.GetActiveSystemCount());

        var system2 = new TestSystemB();
        worldManager.AddSystem(system2);
        worldManager.Update(0f);
        Assert.Equal(2, worldManager.GetActiveSystemCount());
    }

    [Fact]
    public void GetActiveSystemCount_WithDisabledSystem_ShouldReturnCorrectCount()
    {
        using var worldManager = new WorldManager(new SystemManager());

        var system1 = new TestSystemA();
        worldManager.AddSystem(system1);

        var system2 = new TestSystemB();
        worldManager.AddSystem(system2);

        worldManager.Update(0f);
        Assert.Equal(2, worldManager.GetActiveSystemCount());

        worldManager.SetSystemEnabled<TestSystemB>(false);
        worldManager.Update(0f);
        Assert.Equal(1, worldManager.GetActiveSystemCount());
    }

    [Fact]
    public void GetComponentCount_ShouldReturnCorrectCount()
    {
        using var worldManager = new WorldManager(new SystemManager());

        Assert.Equal(0, worldManager.GetComponentCount<TestComponent>());

        var entity1 = worldManager.CreateEntity();
        worldManager.AddComponent(entity1, new TestComponent { Value = 1 });
        Assert.Equal(1, worldManager.GetComponentCount<TestComponent>());

        var entity2 = worldManager.CreateEntity();
        worldManager.AddComponent(entity2, new TestComponent { Value = 2 });
        Assert.Equal(2, worldManager.GetComponentCount<TestComponent>());

        worldManager.RemoveComponent<TestComponent>(entity1);
        Assert.Equal(1, worldManager.GetComponentCount<TestComponent>());
    }

    [Fact]
    public void GetComponentCount_WithMultipleComponentTypes_ShouldReturnCorrectCounts()
    {
        using var worldManager = new WorldManager(new SystemManager());
        var entity = worldManager.CreateEntity();

        worldManager.AddComponent(entity, new TestComponent { Value = 1 });
        worldManager.AddComponent(entity, new TestComponent2 { Value = 2.0f });

        Assert.Equal(1, worldManager.GetComponentCount<TestComponent>());
        Assert.Equal(1, worldManager.GetComponentCount<TestComponent2>());
        Assert.Equal(0, worldManager.GetComponentCount<TestComponent3>());
    }

    [Fact]
    public void AddComponent_ShouldInvalidateQueries()
    {
        using var worldManager = new WorldManager(new SystemManager());
        var entity = worldManager.CreateEntity();
        var query = worldManager.CreateQuery<TestComponent>();

        Assert.Equal(0, query.Count);

        worldManager.AddComponent(entity, new TestComponent { Value = 1 });

        Assert.Equal(1, query.Count);
        Assert.False(query.IsEmpty);
    }

    [Fact]
    public void RemoveComponent_ShouldInvalidateQueries()
    {
        using var worldManager = new WorldManager(new SystemManager());
        var entity1 = worldManager.CreateEntity();
        var entity2 = worldManager.CreateEntity();
        worldManager.AddComponent(entity1, new TestComponent { Value = 1 });
        worldManager.AddComponent(entity2, new TestComponent { Value = 2 });

        var query = worldManager.CreateQuery<TestComponent>();
        Assert.Equal(2, query.Count);

        worldManager.RemoveComponent<TestComponent>(entity1);

        Assert.Equal(1, query.Count);
    }

    [Fact]
    public void DestroyEntity_ShouldInvalidateQueries()
    {
        using var worldManager = new WorldManager(new SystemManager());
        var entity1 = worldManager.CreateEntity();
        var entity2 = worldManager.CreateEntity();
        worldManager.AddComponent(entity1, new TestComponent { Value = 1 });
        worldManager.AddComponent(entity2, new TestComponent { Value = 2 });

        var query = worldManager.CreateQuery<TestComponent>();
        Assert.Equal(2, query.Count);

        worldManager.DestroyEntity(entity1);

        Assert.Equal(1, query.Count);
    }

    [Fact]
    public void AddComponents_ShouldInvalidateQueries()
    {
        using var worldManager = new WorldManager(new SystemManager());
        var entity1 = worldManager.CreateEntity();
        var entity2 = worldManager.CreateEntity();
        var query = worldManager.CreateQuery<TestComponent>();

        Assert.Equal(0, query.Count);

        var components = new[]
        {
            (entity1, new TestComponent { Value = 1 }),
            (entity2, new TestComponent { Value = 2 })
        };

        worldManager.AddComponents<TestComponent>(components);

        Assert.Equal(2, query.Count);
    }

    [Fact]
    public void Query_WithMultipleComponents_ShouldReturnCorrectEntities()
    {
        using var worldManager = new WorldManager(new SystemManager());
        var entity1 = worldManager.CreateEntity();
        var entity2 = worldManager.CreateEntity();
        var entity3 = worldManager.CreateEntity();

        worldManager.AddComponent(entity1, new TestComponent { Value = 1 });
        worldManager.AddComponent(entity1, new TestComponent2 { Value = 1.0f });

        worldManager.AddComponent(entity2, new TestComponent { Value = 2 });

        worldManager.AddComponent(entity3, new TestComponent2 { Value = 3.0f });

        var query = worldManager.CreateQuery<TestComponent, TestComponent2>();

        Assert.Equal(1, query.Count);
        Assert.False(query.IsEmpty);

        var hasFirst = query.TryGetFirst(out var entity, out var comp1, out var comp2);
        Assert.True(hasFirst);
        Assert.Equal(entity1, entity);
        Assert.Equal(1, comp1.Value);
        Assert.Equal(1.0f, comp2.Value);
    }

    [Fact]
    public void Query_WithThreeComponents_ShouldReturnCorrectEntities()
    {
        using var worldManager = new WorldManager(new SystemManager());
        var entity1 = worldManager.CreateEntity();
        var entity2 = worldManager.CreateEntity();

        worldManager.AddComponent(entity1, new TestComponent { Value = 1 });
        worldManager.AddComponent(entity1, new TestComponent2 { Value = 1.0f });
        worldManager.AddComponent(entity1, new TestComponent3 { Value = "test" });

        worldManager.AddComponent(entity2, new TestComponent { Value = 2 });
        worldManager.AddComponent(entity2, new TestComponent2 { Value = 2.0f });

        var query = worldManager.CreateQuery<TestComponent, TestComponent2, TestComponent3>();

        Assert.Equal(1, query.Count);
        Assert.False(query.IsEmpty);

        var hasFirst = query.TryGetFirst(out var entity, out var comp1, out var comp2, out var comp3);
        Assert.True(hasFirst);
        Assert.Equal(entity1, entity);
        Assert.Equal(1, comp1.Value);
        Assert.Equal(1.0f, comp2.Value);
        Assert.Equal("test", comp3.Value);
    }

    [Fact]
    public void Query_Enumeration_ShouldIterateOverMatchingEntities()
    {
        using var worldManager = new WorldManager(new SystemManager());
        var entities = new List<Entity>();

        for (var i = 0; i < 3; i++)
        {
            var entity = worldManager.CreateEntity();
            worldManager.AddComponent(entity, new TestComponent { Value = i });
            entities.Add(entity);
        }

        var query = worldManager.CreateQuery<TestComponent>();
        Assert.Equal(3, query.Count);

        var enumeratedEntities = new List<Entity>();
        var enumeratedValues = new List<int>();

        foreach (var (entity, component) in query)
        {
            enumeratedEntities.Add(entity);
            enumeratedValues.Add(component.Value);
        }

        Assert.Equal(3, enumeratedEntities.Count);
        Assert.Equal(3, enumeratedValues.Count);

        Assert.All(entities, entity => Assert.Contains(entity, enumeratedEntities));
    }

    [Fact]
    public void Query_TryGetFirst_WithEmptyQuery_ShouldReturnFalse()
    {
        using var worldManager = new WorldManager(new SystemManager());
        var query = worldManager.CreateQuery<TestComponent>();

        var hasFirst = query.TryGetFirst(out var entity, out var component);

        Assert.False(hasFirst);
        Assert.Equal(Entity.Null, entity);
        Assert.Equal(default, component);
    }

    [Fact]
    public void Query_Clear_ShouldInvalidateAndEmpty()
    {
        using var worldManager = new WorldManager(new SystemManager());
        var entity = worldManager.CreateEntity();
        worldManager.AddComponent(entity, new TestComponent { Value = 1 });

        var query = worldManager.CreateQuery<TestComponent>();
        Assert.Equal(1, query.Count);

        query.Clear();

        Assert.Equal(1, query.Count);
    }

    [Fact]
    public void DestroyEntities_ShouldInvalidateQueries()
    {
        using var worldManager = new WorldManager(new SystemManager());
        var entities = new Entity[3];
        for (var i = 0; i < 3; i++)
        {
            entities[i] = worldManager.CreateEntity();
            worldManager.AddComponent(entities[i], new TestComponent { Value = i });
        }

        var query = worldManager.CreateQuery<TestComponent>();
        Assert.Equal(3, query.Count);

        worldManager.DestroyEntities(entities.AsSpan(0, 2));

        Assert.Equal(1, query.Count);
    }

    [Fact]
    public void AddComponent_SameComponentTwice_ShouldOverwriteExisting()
    {
        using var worldManager = new WorldManager(new SystemManager());
        var entity = worldManager.CreateEntity();

        worldManager.AddComponent(entity, new TestComponent { Value = 1 });
        Assert.Equal(1, worldManager.GetComponent<TestComponent>(entity).Value);

        worldManager.SetComponent(entity, new TestComponent { Value = 2 });

        Assert.Equal(2, worldManager.GetComponent<TestComponent>(entity).Value);
        Assert.Equal(1, worldManager.GetComponentCount<TestComponent>());
    }

    [Fact]
    public void ComponentManager_GetAllComponents_ShouldReturnAllStoredValues()
    {
        using var worldManager = new WorldManager(new SystemManager());
        var entity1 = worldManager.CreateEntity();
        var entity2 = worldManager.CreateEntity();

        worldManager.AddComponent(entity1, new TestComponent { Value = 1 });
        worldManager.AddComponent(entity2, new TestComponent { Value = 2 });

        var components = worldManager.ComponentManager.GetAllComponents<TestComponent>();

        Assert.Equal(2, components.Length);
        var values = new[] { components[0].Value, components[1].Value };
        Assert.Contains(1, values);
        Assert.Contains(2, values);
    }

    [Fact]
    public void ComponentManager_GetAllComponents_WithFragmentedStorage_ShouldReturnOnlyRemainingActiveValues()
    {
        using var worldManager = new WorldManager(new SystemManager());
        var entity1 = worldManager.CreateEntity();
        var entity2 = worldManager.CreateEntity();
        var entity3 = worldManager.CreateEntity();

        worldManager.AddComponent(entity1, new TestComponent { Value = 1 });
        worldManager.AddComponent(entity2, new TestComponent { Value = 2 });
        worldManager.AddComponent(entity3, new TestComponent { Value = 3 });

        worldManager.RemoveComponent<TestComponent>(entity1);

        var components = worldManager.ComponentManager.GetAllComponents<TestComponent>();

        Assert.Equal(2, components.Length);
        var values = new List<int>();
        foreach (var component in components)
            values.Add(component.Value);

        Assert.Equal([2, 3], values.OrderBy(v => v));
        Assert.DoesNotContain(0, values);
        Assert.Contains(3, values);
    }

    [Fact]
    public void ComponentManager_GetAllComponents_DoesNotClaimWritability()
    {
        using var worldManager = new WorldManager(new SystemManager());
        var entity = worldManager.CreateEntity();
        worldManager.AddComponent(entity, new TestComponent { Value = 1 });

        ReadOnlySpan<TestComponent> components = worldManager.ComponentManager.GetAllComponents<TestComponent>();

        Assert.Equal(1, components.Length);
        Assert.Equal(1, components[0].Value);
    }

    [Fact]
    public void ComponentManager_GetAllComponents_WithNoStorage_ShouldReturnEmptySpan()
    {
        using var worldManager = new WorldManager(new SystemManager());

        var components = worldManager.ComponentManager.GetAllComponents<TestComponent>();

        Assert.True(components.IsEmpty);
    }

    [Fact]
    public void Dispose_ShouldPreventCreateEntityOperation()
    {
        var worldManager = new WorldManager(new SystemManager());
        var entity = worldManager.CreateEntity();
        worldManager.AddComponent(entity, new TestComponent { Value = 42 });

        worldManager.Dispose();

        var exception = Record.Exception(() => worldManager.Dispose());
        Assert.Null(exception);

        Assert.Throws<ObjectDisposedException>(() => worldManager.CreateEntity());
    }

    [Fact]
    public void Dispose_ShouldMakeAllOperationsReturnFalse()
    {
        var worldManager = new WorldManager(new SystemManager());
        var entity = worldManager.CreateEntity();
        worldManager.AddComponent(entity, new TestComponent { Value = 42 });

        Assert.True(worldManager.HasComponent<TestComponent>(entity));
        Assert.True(worldManager.TryGetComponent<TestComponent>(entity, out var componentBefore));
        Assert.Equal(42, componentBefore.Value);

        worldManager.Dispose();

        var hasComponent = worldManager.HasComponent<TestComponent>(entity);
        Assert.False(hasComponent);

        var tryGetResult = worldManager.TryGetComponent<TestComponent>(entity, out var component);
        Assert.False(tryGetResult);
        Assert.Equal(default, component);

        var destroyException = Record.Exception(() => worldManager.DestroyEntity(entity));
        Assert.Null(destroyException);
    }

    [Fact]
    public void Dispose_MultipleCallsShouldNotThrow()
    {
        var worldManager = new WorldManager(new SystemManager());
        var entity = worldManager.CreateEntity();
        worldManager.AddComponent(entity, new TestComponent { Value = 42 });

        worldManager.Dispose();
        worldManager.Dispose();
        worldManager.Dispose();

        Assert.True(true);
    }

    [Fact]
    public void Dispose_WithSystems_ShouldNotDisposeSystemsOwnedByTheSharedSystemManager()
    {
        var worldManager = new WorldManager(new SystemManager());
        var system1 = new TestDisposableSystem1();
        var system2 = new TestDisposableSystem2();

        worldManager.AddSystem(system1);
        worldManager.AddSystem(system2);

        worldManager.Dispose();

        Assert.False(system1.WasDisposed);
        Assert.False(system2.WasDisposed);
    }

    [Fact]
    public void CreateEntity_OnDisposedWorldManager_ShouldThrowObjectDisposedException()
    {
        var worldManager = new WorldManager(new SystemManager());
        worldManager.Dispose();

        Assert.Throws<ObjectDisposedException>(() => worldManager.CreateEntity());
    }

    [Fact]
    public void Operations_OnDisposedWorldManager_MixedBehavior()
    {
        var worldManager = new WorldManager(new SystemManager());
        var entity = worldManager.CreateEntity();
        worldManager.AddComponent(entity, new TestComponent { Value = 42 });
        worldManager.Dispose();

        Assert.Throws<ObjectDisposedException>(() => worldManager.CreateEntity());

        var hasComponentException = Record.Exception(() => worldManager.HasComponent<TestComponent>(entity));
        Assert.Null(hasComponentException);

        var tryGetException = Record.Exception(() => worldManager.TryGetComponent<TestComponent>(entity, out _));
        Assert.Null(tryGetException);

        var destroyException = Record.Exception(() => worldManager.DestroyEntity(entity));
        Assert.Null(destroyException);

        var system = new TestSystemA();
        var addSystemException = Record.Exception(() => worldManager.AddSystem(system));
        Assert.Null(addSystemException);

        var updateException = Record.Exception(() => worldManager.Update(0.16f));
        Assert.Null(updateException);
    }

    [Fact]
    public void AddSystem_WithPriority_ShouldExecuteInCorrectOrder()
    {
        using var worldManager = new WorldManager(new SystemManager());
        var executionOrder = new List<string>();

        var highPrioritySystem = new HighPriorityOrderTestSystem("High", executionOrder);
        var lowPrioritySystem = new LowPriorityOrderTestSystem("Low", executionOrder);
        var mediumPrioritySystem = new MediumPriorityOrderTestSystem("Medium", executionOrder);

        worldManager.AddSystem(lowPrioritySystem, 1);
        worldManager.AddSystem(highPrioritySystem, 3);
        worldManager.AddSystem(mediumPrioritySystem, 2);

        worldManager.Update(0.16f);

        Assert.Equal(3, executionOrder.Count);
        Assert.Equal("High", executionOrder[0]);
        Assert.Equal("Medium", executionOrder[1]);
        Assert.Equal("Low", executionOrder[2]);
    }


    [Fact]
    public void AddSystem_WithSamePriority_ShouldExecuteInAddOrder()
    {
        using var worldManager = new WorldManager(new SystemManager());
        var executionOrder = new List<string>();

        var system1 = new FirstOrderTestSystem("First", executionOrder);
        var system2 = new SecondOrderTestSystem("Second", executionOrder);
        var system3 = new ThirdOrderTestSystem("Third", executionOrder);

        worldManager.AddSystem(system1, 1);
        worldManager.AddSystem(system2, 1);
        worldManager.AddSystem(system3, 1);

        worldManager.Update(0.16f);

        Assert.Equal(3, executionOrder.Count);
        Assert.Equal("First", executionOrder[0]);
        Assert.Equal("Second", executionOrder[1]);
        Assert.Equal("Third", executionOrder[2]);
    }

    private class TestDisposableSystem1 : ISystem {
        public bool WasDisposed { get; private set; }

        public void Initialize(WorldManager world)
        {
        }

        public void Update(float deltaTime)
        {
        }

        public void Dispose()
            => WasDisposed = true;
    }

    private class TestDisposableSystem2 : ISystem {
        public bool WasDisposed { get; private set; }

        public void Initialize(WorldManager world)
        {
        }

        public void Update(float deltaTime)
        {
        }

        public void Dispose()
            => WasDisposed = true;
    }

    private class HighPriorityOrderTestSystem(string name, List<string> executionOrder) : ISystem {

        public void Initialize(WorldManager world)
        {
        }

        public void Update(float deltaTime)
            => executionOrder.Add(name);

        public void Dispose()
        {
        }
    }

    private class MediumPriorityOrderTestSystem(string name, List<string> executionOrder) : ISystem {

        public void Initialize(WorldManager world)
        {
        }

        public void Update(float deltaTime)
            => executionOrder.Add(name);

        public void Dispose()
        {
        }
    }

    private class LowPriorityOrderTestSystem(string name, List<string> executionOrder) : ISystem {

        public void Initialize(WorldManager world)
        {
        }

        public void Update(float deltaTime)
            => executionOrder.Add(name);

        public void Dispose()
        {
        }
    }

    private class FirstOrderTestSystem(string name, List<string> executionOrder) : ISystem {

        public void Initialize(WorldManager world)
        {
        }

        public void Update(float deltaTime)
            => executionOrder.Add(name);

        public void Dispose()
        {
        }
    }

    private class SecondOrderTestSystem(string name, List<string> executionOrder) : ISystem {

        public void Initialize(WorldManager world)
        {
        }

        public void Update(float deltaTime)
            => executionOrder.Add(name);

        public void Dispose()
        {
        }
    }

    private class ThirdOrderTestSystem(string name, List<string> executionOrder) : ISystem {

        public void Initialize(WorldManager world)
        {
        }

        public void Update(float deltaTime)
            => executionOrder.Add(name);

        public void Dispose()
        {
        }
    }

    private struct TestComponent : IComponent {
        public int Value;
    }

    private struct TestComponent2 : IComponent {
        public float Value;
    }

    private struct TestComponent3 : IComponent {
        public string Value;
    }

    public abstract class TestSystem : ISystem {
        public abstract void Initialize(WorldManager world);
        public abstract void Update(float deltaTime);
        public abstract void Dispose();
    }

    private class TestSystemA : ISystem {
        public void Initialize(WorldManager world)
        {
        }

        public void Update(float deltaTime)
        {
        }

        public void Dispose()
        {
        }
    }

    private class TestSystemB : ISystem {
        public void Initialize(WorldManager world)
        {
        }

        public void Update(float deltaTime)
        {
        }

        public void Dispose()
        {
        }
    }
}