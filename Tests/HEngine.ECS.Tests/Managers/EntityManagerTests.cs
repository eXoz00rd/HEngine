using HEngine.Core.Managers;
using HEngine.Core.Primitives;

namespace HEngine.ECS.Tests.Managers;

public class EntityManagerTests : IDisposable {
    private readonly EntityManager _entityManager = new();

    public void Dispose()
        => _entityManager.Dispose();

    [Fact]
    public void IsEntityValid_AfterDestruction_ShouldReturnFalseImmediately()
    {
        var entity = _entityManager.CreateEntity();

        _entityManager.DestroyEntity(entity);

        Assert.False(_entityManager.IsEntityValid(entity));
    }

    [Fact]
    public void DestroyEntity_CalledTwice_ShouldNotRecycleTheSameIdTwice()
    {
        var entity = _entityManager.CreateEntity();

        _entityManager.DestroyEntity(entity);
        _entityManager.DestroyEntity(entity);

        var recycled1 = _entityManager.CreateEntity();
        var recycled2 = _entityManager.CreateEntity();

        Assert.NotEqual(recycled1.Id, recycled2.Id);
    }

    [Fact]
    public void CreateEntity_AfterDestruction_ShouldReuseIdWithIncrementedGeneration()
    {
        var entity = _entityManager.CreateEntity();
        _entityManager.DestroyEntity(entity);

        var recycled = _entityManager.CreateEntity();

        Assert.Equal(entity.Id, recycled.Id);
        Assert.Equal(entity.Generation + 1, recycled.Generation);
        Assert.False(_entityManager.IsEntityValid(entity));
        Assert.True(_entityManager.IsEntityValid(recycled));
    }

    [Fact]
    public void GetAllActiveEntities_AfterDestruction_ShouldNotContainDestroyedEntity()
    {
        var entity1 = _entityManager.CreateEntity();
        var entity2 = _entityManager.CreateEntity();

        _entityManager.DestroyEntity(entity1);

        var active = _entityManager.GetAllActiveEntities().ToList();

        Assert.DoesNotContain(entity1, active);
        Assert.Contains(entity2, active);
    }

    [Fact]
    public void IsEntityActive_AfterDestruction_ShouldReturnFalse()
    {
        var entity = _entityManager.CreateEntity();

        _entityManager.DestroyEntity(entity);

        Assert.False(_entityManager.IsEntityActive(entity.Id));
    }
}
