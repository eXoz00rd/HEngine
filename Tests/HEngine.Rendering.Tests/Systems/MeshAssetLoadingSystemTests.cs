using System.Numerics;
using HEngine.Assets.Assets;
using HEngine.Core.Managers;
using HEngine.Core.Primitives;
using HEngine.Core.Rendering.Data;
using HEngine.Rendering.Components;
using HEngine.Rendering.Systems;

namespace HEngine.Rendering.Tests.Systems;

public class MeshAssetLoadingSystemTests : IDisposable
{
    private readonly AssetManager _assetManager;
    private readonly MeshAssetLoadingSystem _system;
    private readonly WorldManager _world;

    public MeshAssetLoadingSystemTests()
    {
        _world = new WorldManager(new SystemManager());
        _assetManager = CreateMockAssetManager();
        _system = new MeshAssetLoadingSystem();
        _system.Initialize(_world, _assetManager);
    }

    public void Dispose()
    {
        _system?.Dispose();
        _world?.Dispose();
    }

    [Fact]
    public async Task Update_WithNotLoadedAsset_StartsLoading()
    {
        var entity = _world.CreateEntity();
        _world.AddComponent(entity, new MeshAsset("test.mesh"));

        _system.Update(0.016f);

        await Task.Delay(50);

        var asset = _world.GetComponent<MeshAsset>(entity);
        Assert.True(asset.LoadState == AssetLoadState.Loading || asset.LoadState == AssetLoadState.Loaded);
    }

    [Fact]
    public async Task Update_WaitsForLoad_AssetBecomesLoaded()
    {
        var entity = _world.CreateEntity();
        _world.AddComponent(entity, new MeshAsset("test.mesh"));

        _system.Update(0.016f);

        await Task.Delay(200);
        _system.Update(0.016f);

        var asset = _world.GetComponent<MeshAsset>(entity);
        Assert.Equal(AssetLoadState.Loaded, asset.LoadState);
        Assert.NotNull(asset.Vertices);
        Assert.NotNull(asset.Indices);
        Assert.True(asset.IsLoaded);
    }

    [Fact]
    public async Task Update_WithFailingLoad_AssetStateIsFailed()
    {
        var failingAssetManager = new AssetManager(async path =>
        {
            await Task.Delay(10);
            throw new FileNotFoundException("Test file not found");
        });

        var system = new MeshAssetLoadingSystem();
        system.Initialize(_world, failingAssetManager);

        var entity = _world.CreateEntity();
        _world.AddComponent(entity, new MeshAsset("nonexistent.mesh"));

        system.Update(0.016f);

        await Task.Delay(200);
        system.Update(0.016f);

        var asset = _world.GetComponent<MeshAsset>(entity);
        Assert.Equal(AssetLoadState.Failed, asset.LoadState);
        Assert.True(asset.HasFailed);
        Assert.NotNull(asset.ErrorMessage);

        system.Dispose();
        failingAssetManager.Dispose();
    }

    [Fact]
    public async Task Update_MultipleEntities_AllLoadConcurrently()
    {
        var entities = new List<Entity>();

        for (var i = 0; i < 5; i++)
        {
            var entity = _world.CreateEntity();
            _world.AddComponent(entity, new MeshAsset($"mesh{i}.mesh"));
            entities.Add(entity);
        }

        _system.Update(0.016f);

        await Task.Delay(300);
        _system.Update(0.016f);

        foreach (var entity in entities)
        {
            if (_world.HasComponent<MeshAsset>(entity))
            {
                var asset = _world.GetComponent<MeshAsset>(entity);
                Assert.Equal(AssetLoadState.Loaded, asset.LoadState);
            }
        }
    }

    [Fact]
    public void Initialize_WithNullWorld_ThrowsArgumentNullException()
    {
        var system = new MeshAssetLoadingSystem();
        Assert.Throws<ArgumentNullException>(() => system.Initialize(null!, _assetManager));
    }

    [Fact]
    public void Initialize_WithNullAssetManager_ThrowsArgumentNullException()
    {
        var system = new MeshAssetLoadingSystem();
        Assert.Throws<ArgumentNullException>(() => system.Initialize(_world, null!));
    }

    [Fact]
    public async Task Update_AfterEntityRemoved_DoesNotCrash()
    {
        var entity = _world.CreateEntity();
        _world.AddComponent(entity, new MeshAsset("test.mesh"));

        _system.Update(0.016f);
        _world.DestroyEntity(entity);

        await Task.Delay(200);

        _system.Update(0.016f);
    }

    [Fact]
    public async Task Update_SamePathMultipleTimes_UsesCache()
    {
        var loadCount = 0;
        var countingAssetManager = new AssetManager(async path =>
        {
            Interlocked.Increment(ref loadCount);
            await Task.Delay(50);
            return new LoadedMesh(CreateTestVertices(), new uint[] { 0, 1, 2 });
        });

        var system = new MeshAssetLoadingSystem();
        system.Initialize(_world, countingAssetManager);

        var entity1 = _world.CreateEntity();
        var entity2 = _world.CreateEntity();
        var entity3 = _world.CreateEntity();

        _world.AddComponent(entity1, new MeshAsset("same.mesh"));
        _world.AddComponent(entity2, new MeshAsset("same.mesh"));
        _world.AddComponent(entity3, new MeshAsset("same.mesh"));

        system.Update(0.016f);

        await Task.Delay(300);
        system.Update(0.016f);

        Assert.Equal(1, loadCount);

        var asset1 = _world.GetComponent<MeshAsset>(entity1);
        var asset2 = _world.GetComponent<MeshAsset>(entity2);
        var asset3 = _world.GetComponent<MeshAsset>(entity3);

        Assert.Equal(AssetLoadState.Loaded, asset1.LoadState);
        Assert.Equal(AssetLoadState.Loaded, asset2.LoadState);
        Assert.Equal(AssetLoadState.Loaded, asset3.LoadState);

        system.Dispose();
        countingAssetManager.Dispose();
    }

    [Fact]
    public void MeshAsset_IsLoaded_ReturnsTrueWhenLoadedWithData()
    {
        var asset = new MeshAsset("test.mesh")
        {
            LoadState = AssetLoadState.Loaded,
            Vertices = CreateTestVertices(),
            Indices = new uint[] { 0, 1, 2 }
        };

        Assert.True(asset.IsLoaded);
    }

    [Fact]
    public void MeshAsset_IsLoaded_ReturnsFalseWhenNotLoaded()
    {
        var asset = new MeshAsset("test.mesh");

        Assert.False(asset.IsLoaded);
    }

    [Fact]
    public void MeshAsset_HasFailed_ReturnsTrueWhenFailed()
    {
        var asset = new MeshAsset("test.mesh")
        {
            LoadState = AssetLoadState.Failed,
            ErrorMessage = "Test error"
        };

        Assert.True(asset.HasFailed);
    }

    [Fact]
    public void MeshAsset_IsLoading_ReturnsTrueWhenLoading()
    {
        var asset = new MeshAsset("test.mesh")
        {
            LoadState = AssetLoadState.Loading
        };

        Assert.True(asset.IsLoading);
    }

    [Fact]
    public void MeshAsset_Constructor_ThrowsOnNullPath()
    {
        Assert.Throws<ArgumentNullException>(() => new MeshAsset(null!));
    }

    [Fact]
    public async Task Dispose_WaitsForLoadingTasks_CompletesGracefully()
    {
        var entity = _world.CreateEntity();
        _world.AddComponent(entity, new MeshAsset("test.mesh"));

        _system.Update(0.016f);

        _system.Dispose();

        var asset = _world.GetComponent<MeshAsset>(entity);
        Assert.True(asset.LoadState == AssetLoadState.Loading || asset.LoadState == AssetLoadState.Loaded);
    }

    [Fact]
    public async Task Update_RapidSuccessiveCalls_DoesNotStartDuplicateLoads()
    {
        var entity = _world.CreateEntity();
        _world.AddComponent(entity, new MeshAsset("test.mesh"));

        _system.Update(0.016f);
        _system.Update(0.016f);
        _system.Update(0.016f);

        await Task.Delay(200);

        var asset = _world.GetComponent<MeshAsset>(entity);
        Assert.Equal(AssetLoadState.Loaded, asset.LoadState);
    }

    private static AssetManager CreateMockAssetManager()
    {
        return new AssetManager(path =>
        {
            return Task.FromResult(new LoadedMesh(CreateTestVertices(), new uint[] { 0, 1, 2, 2, 1, 3 }));
        });
    }

    private static Vertex3D[] CreateTestVertices()
    {
        return new[]
        {
            new Vertex3D(
                new Vector3(0, 0, 0),
                new Vector3(0, 1, 0),
                new Vector2(0, 0),
                new Vector4(1, 0, 0, 1)
            ),
            new Vertex3D(
                new Vector3(1, 0, 0),
                new Vector3(0, 1, 0),
                new Vector2(1, 0),
                new Vector4(0, 1, 0, 1)
            ),
            new Vertex3D(
                new Vector3(0, 0, 1),
                new Vector3(0, 1, 0),
                new Vector2(0, 1),
                new Vector4(0, 0, 1, 1)
            )
        };
    }
}