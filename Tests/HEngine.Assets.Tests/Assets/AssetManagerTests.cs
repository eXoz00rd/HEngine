using System.Numerics;
using HEngine.Assets.Assets;
using HEngine.Core.Rendering.Data;

namespace HEngine.Core.Tests.Assets;

public class AssetManagerTests : IDisposable
{
    private readonly AssetManager _assetManager;
    private readonly string _testDirectory;

    public AssetManagerTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "HEngineAssetTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_testDirectory);
        _assetManager = new AssetManager(CreateMockLoader());
    }

    public void Dispose()
    {
        _assetManager?.Dispose();
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public void Constructor_NullMeshLoader_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new AssetManager(null!));
    }

    [Fact]
    public async Task LoadMeshAsync_ValidId_LoadsSuccessfully()
    {
        var id = _assetManager.Import(CreateTestMeshFile("test.mesh"));

        var mesh = await _assetManager.LoadMeshAsync(id);

        Assert.NotNull(mesh);
        Assert.NotNull(mesh.Vertices);
        Assert.NotNull(mesh.Indices);
        Assert.NotEmpty(mesh.Vertices);
    }

    [Fact]
    public async Task LoadMeshAsync_SameId_ReturnsCachedAsset()
    {
        var id = _assetManager.Import(CreateTestMeshFile("cached.mesh"));

        var mesh1 = await _assetManager.LoadMeshAsync(id);
        var mesh2 = await _assetManager.LoadMeshAsync(id);

        Assert.Same(mesh1, mesh2);
        Assert.Equal(1, _assetManager.LoadedAssetCount);
    }

    [Fact]
    public async Task LoadMeshAsync_SameId_IncrementsRefCount()
    {
        var id = _assetManager.Import(CreateTestMeshFile("refcount.mesh"));

        await _assetManager.LoadMeshAsync(id);
        Assert.Equal(1, _assetManager.GetRefCount(id));

        await _assetManager.LoadMeshAsync(id);
        Assert.Equal(2, _assetManager.GetRefCount(id));

        await _assetManager.LoadMeshAsync(id);
        Assert.Equal(3, _assetManager.GetRefCount(id));
    }

    [Fact]
    public async Task Unload_DecrementRefCount_RemovesWhenZero()
    {
        var id = _assetManager.Import(CreateTestMeshFile("unload.mesh"));

        await _assetManager.LoadMeshAsync(id);
        await _assetManager.LoadMeshAsync(id);
        Assert.Equal(2, _assetManager.GetRefCount(id));

        _assetManager.Unload(id);
        Assert.Equal(1, _assetManager.GetRefCount(id));
        Assert.True(_assetManager.IsLoaded(id));

        _assetManager.Unload(id);
        Assert.Equal(0, _assetManager.GetRefCount(id));
        Assert.False(_assetManager.IsLoaded(id));
    }

    [Fact]
    public async Task IsLoaded_AfterLoading_ReturnsTrue()
    {
        var id = _assetManager.Import(CreateTestMeshFile("loaded.mesh"));

        Assert.False(_assetManager.IsLoaded(id));

        await _assetManager.LoadMeshAsync(id);

        Assert.True(_assetManager.IsLoaded(id));
    }

    [Fact]
    public async Task UnloadAll_RemovesAllAssets()
    {
        var id1 = _assetManager.Import(CreateTestMeshFile("asset1.mesh"));
        var id2 = _assetManager.Import(CreateTestMeshFile("asset2.mesh"));
        var id3 = _assetManager.Import(CreateTestMeshFile("asset3.mesh"));

        await _assetManager.LoadMeshAsync(id1);
        await _assetManager.LoadMeshAsync(id2);
        await _assetManager.LoadMeshAsync(id3);

        Assert.Equal(3, _assetManager.LoadedAssetCount);

        _assetManager.UnloadAll();

        Assert.Equal(0, _assetManager.LoadedAssetCount);
        Assert.False(_assetManager.IsLoaded(id1));
        Assert.False(_assetManager.IsLoaded(id2));
        Assert.False(_assetManager.IsLoaded(id3));
    }

    [Fact]
    public async Task LoadMeshAsync_UnimportedId_ThrowsKeyNotFoundException()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _assetManager.LoadMeshAsync(AssetId.New()));
    }

    [Fact]
    public async Task LoadMeshAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        var id = _assetManager.Import(CreateTestMeshFile("disposed.mesh"));
        _assetManager.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => _assetManager.LoadMeshAsync(id));
    }

    [Fact]
    public async Task LoadMeshAsync_ConcurrentRequests_LoadsOnlyOnce()
    {
        var loadCount = 0;

        var manager = new AssetManager(async p =>
        {
            Interlocked.Increment(ref loadCount);
            await Task.Delay(100);
            return new LoadedMesh(CreateTestVertices(), new uint[] { 0, 1, 2 });
        });
        var id = manager.Import(CreateTestMeshFile("concurrent.mesh"));

        var tasks = new Task<LoadedMesh>[10];
        for (var i = 0; i < tasks.Length; i++)
        {
            tasks[i] = manager.LoadMeshAsync(id);
        }

        var results = await Task.WhenAll(tasks);

        Assert.Equal(1, loadCount);
        Assert.Equal(1, manager.LoadedAssetCount);

        for (var i = 1; i < results.Length; i++)
        {
            Assert.Same(results[0], results[i]);
        }

        manager.Dispose();
    }

    [Fact]
    public async Task LoadMeshAsync_DifferentPaths_LoadsSeparately()
    {
        var id1 = _assetManager.Import(CreateTestMeshFile("mesh1.mesh"));
        var id2 = _assetManager.Import(CreateTestMeshFile("mesh2.mesh"));

        var mesh1 = await _assetManager.LoadMeshAsync(id1);
        var mesh2 = await _assetManager.LoadMeshAsync(id2);

        Assert.NotSame(mesh1, mesh2);
        Assert.Equal(2, _assetManager.LoadedAssetCount);
    }

    [Fact]
    public void Import_PathNormalization_ReturnsSameId()
    {
        var path = CreateTestMeshFile("normalize.mesh");

        var id1 = _assetManager.Import(path.ToLower());
        var id2 = _assetManager.Import(path.ToUpper());

        Assert.Equal(id1, id2);
    }

    [Fact]
    public void Import_NullOrEmptyPath_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _assetManager.Import(null!));
        Assert.Throws<ArgumentException>(() => _assetManager.Import(""));
        Assert.Throws<ArgumentException>(() => _assetManager.Import("   "));
    }

    [Fact]
    public void IsLoaded_UnimportedId_ReturnsFalse()
    {
        Assert.False(_assetManager.IsLoaded(AssetId.New()));
    }

    [Fact]
    public void IsLoading_UnimportedId_ReturnsFalse()
    {
        Assert.False(_assetManager.IsLoading(AssetId.New()));
    }

    [Fact]
    public void GetRefCount_UnimportedId_ReturnsZero()
    {
        Assert.Equal(0, _assetManager.GetRefCount(AssetId.New()));
    }

    [Fact]
    public void Unload_UnimportedId_DoesNotThrow()
    {
        _assetManager.Unload(AssetId.New());
    }

    [Fact]
    public void Move_RepointsPath_KeepsSameId()
    {
        var oldPath = CreateTestMeshFile("moved-source.mesh");
        var newPath = CreateTestMeshFile("moved-destination.mesh");
        var id = _assetManager.Import(oldPath);

        _assetManager.Move(id, newPath);

        Assert.Equal(newPath, _assetManager.ResolvePath(id));
        Assert.Equal(id, _assetManager.Import(newPath));
    }

    [Fact]
    public void ResolvePath_UnimportedId_ThrowsKeyNotFoundException()
    {
        Assert.Throws<KeyNotFoundException>(() => _assetManager.ResolvePath(AssetId.New()));
    }

    [Fact]
    public void Move_UnimportedId_ThrowsKeyNotFoundException()
    {
        Assert.Throws<KeyNotFoundException>(() => _assetManager.Move(AssetId.New(), CreateTestMeshFile("target.mesh")));
    }

    [Fact]
    public void Move_DestinationOwnedByAnotherId_ThrowsInvalidOperationException()
    {
        var pathA = CreateTestMeshFile("owner-a.mesh");
        var pathB = CreateTestMeshFile("owner-b.mesh");
        var idA = _assetManager.Import(pathA);
        var idB = _assetManager.Import(pathB);

        Assert.Throws<InvalidOperationException>(() => _assetManager.Move(idA, pathB));

        Assert.Equal(pathA, _assetManager.ResolvePath(idA));
        Assert.Equal(pathB, _assetManager.ResolvePath(idB));
        Assert.Equal(idB, _assetManager.Import(pathB));
    }

    [Fact]
    public void Import_RelativePath_ResolvesToAbsolutePath()
    {
        var absolutePath = CreateTestMeshFile("relative-target.mesh");
        var relativePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), absolutePath);

        var id = _assetManager.Import(relativePath);

        Assert.Equal(absolutePath, _assetManager.ResolvePath(id));
        Assert.True(Path.IsPathFullyQualified(_assetManager.ResolvePath(id)));
    }

    [Fact]
    public async Task LoadMeshAsync_MultipleConcurrentDifferentAssets_LoadsAllCorrectly()
    {
        var ids = new[]
        {
            _assetManager.Import(CreateTestMeshFile("multi1.mesh")),
            _assetManager.Import(CreateTestMeshFile("multi2.mesh")),
            _assetManager.Import(CreateTestMeshFile("multi3.mesh")),
            _assetManager.Import(CreateTestMeshFile("multi4.mesh")),
            _assetManager.Import(CreateTestMeshFile("multi5.mesh"))
        };

        var tasks = ids.Select(id => _assetManager.LoadMeshAsync(id)).ToArray();
        var results = await Task.WhenAll(tasks);

        Assert.Equal(5, _assetManager.LoadedAssetCount);
        Assert.All(results, r => Assert.NotNull(r));
    }

    [Fact]
    public async Task Dispose_WhileLoading_CompletesGracefully()
    {
        var manager = new AssetManager(async p =>
        {
            await Task.Delay(200);
            return new LoadedMesh(CreateTestVertices(), new uint[] { 0, 1, 2 });
        });
        var id = manager.Import(CreateTestMeshFile("disposing.mesh"));

        var loadTask = manager.LoadMeshAsync(id);
        await Task.Delay(50);
        manager.Dispose();

        await Task.Delay(300);
    }

    [Fact]
    public async Task Dispose_LoadCompletesAfterDispose_DoesNotLeakIntoCache()
    {
        var manager = new AssetManager(async p =>
        {
            await Task.Delay(100);
            return new LoadedMesh(CreateTestVertices(), new uint[] { 0, 1, 2 });
        });
        var id = manager.Import(CreateTestMeshFile("post-dispose.mesh"));

        var loadTask = manager.LoadMeshAsync(id);
        manager.Dispose();

        await Task.Delay(200);

        Assert.Equal(0, manager.LoadedAssetCount);
    }

    private string CreateTestMeshFile(string filename)
    {
        return Path.Combine(_testDirectory, filename);
    }

    private Func<string, Task<LoadedMesh>> CreateMockLoader()
    {
        return async path =>
        {
            await Task.Delay(10);
            return new LoadedMesh(CreateTestVertices(), new uint[] { 0, 1, 2, 2, 1, 3 });
        };
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
            ),
            new Vertex3D(
                new Vector3(1, 0, 1),
                new Vector3(0, 1, 0),
                new Vector2(1, 1),
                new Vector4(1, 1, 0, 1)
            )
        };
    }
}
