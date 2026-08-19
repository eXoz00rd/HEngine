using System.Numerics;
using HEngine.Core.Assets;
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
    public async Task LoadMeshAsync_ValidPath_LoadsSuccessfully()
    {
        var path = CreateTestMeshFile("test.mesh");

        var mesh = await _assetManager.LoadMeshAsync(path);

        Assert.NotNull(mesh);
        Assert.NotNull(mesh.Vertices);
        Assert.NotNull(mesh.Indices);
        Assert.NotEmpty(mesh.Vertices);
    }

    [Fact]
    public async Task LoadMeshAsync_SamePath_ReturnsCachedAsset()
    {
        var path = CreateTestMeshFile("cached.mesh");

        var mesh1 = await _assetManager.LoadMeshAsync(path);
        var mesh2 = await _assetManager.LoadMeshAsync(path);

        Assert.Same(mesh1, mesh2);
        Assert.Equal(1, _assetManager.LoadedAssetCount);
    }

    [Fact]
    public async Task LoadMeshAsync_SamePath_IncrementsRefCount()
    {
        var path = CreateTestMeshFile("refcount.mesh");

        await _assetManager.LoadMeshAsync(path);
        Assert.Equal(1, _assetManager.GetRefCount(path));

        await _assetManager.LoadMeshAsync(path);
        Assert.Equal(2, _assetManager.GetRefCount(path));

        await _assetManager.LoadMeshAsync(path);
        Assert.Equal(3, _assetManager.GetRefCount(path));
    }

    [Fact]
    public async Task Unload_DecrementRefCount_RemovesWhenZero()
    {
        var path = CreateTestMeshFile("unload.mesh");

        await _assetManager.LoadMeshAsync(path);
        await _assetManager.LoadMeshAsync(path);
        Assert.Equal(2, _assetManager.GetRefCount(path));

        _assetManager.Unload(path);
        Assert.Equal(1, _assetManager.GetRefCount(path));
        Assert.True(_assetManager.IsLoaded(path));

        _assetManager.Unload(path);
        Assert.Equal(0, _assetManager.GetRefCount(path));
        Assert.False(_assetManager.IsLoaded(path));
    }

    [Fact]
    public async Task IsLoaded_AfterLoading_ReturnsTrue()
    {
        var path = CreateTestMeshFile("loaded.mesh");

        Assert.False(_assetManager.IsLoaded(path));

        await _assetManager.LoadMeshAsync(path);

        Assert.True(_assetManager.IsLoaded(path));
    }

    [Fact]
    public async Task UnloadAll_RemovesAllAssets()
    {
        var path1 = CreateTestMeshFile("asset1.mesh");
        var path2 = CreateTestMeshFile("asset2.mesh");
        var path3 = CreateTestMeshFile("asset3.mesh");

        await _assetManager.LoadMeshAsync(path1);
        await _assetManager.LoadMeshAsync(path2);
        await _assetManager.LoadMeshAsync(path3);

        Assert.Equal(3, _assetManager.LoadedAssetCount);

        _assetManager.UnloadAll();

        Assert.Equal(0, _assetManager.LoadedAssetCount);
        Assert.False(_assetManager.IsLoaded(path1));
        Assert.False(_assetManager.IsLoaded(path2));
        Assert.False(_assetManager.IsLoaded(path3));
    }

    [Fact]
    public async Task LoadMeshAsync_NullPath_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _assetManager.LoadMeshAsync(null!));
        await Assert.ThrowsAsync<ArgumentException>(() => _assetManager.LoadMeshAsync(""));
        await Assert.ThrowsAsync<ArgumentException>(() => _assetManager.LoadMeshAsync("   "));
    }

    [Fact]
    public async Task LoadMeshAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        var path = CreateTestMeshFile("disposed.mesh");
        _assetManager.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => _assetManager.LoadMeshAsync(path));
    }

    [Fact]
    public async Task LoadMeshAsync_ConcurrentRequests_LoadsOnlyOnce()
    {
        var path = CreateTestMeshFile("concurrent.mesh");
        var loadCount = 0;

        var manager = new AssetManager(async p =>
        {
            Interlocked.Increment(ref loadCount);
            await Task.Delay(100);
            return new LoadedMesh(CreateTestVertices(), new uint[] { 0, 1, 2 });
        });

        var tasks = new Task<LoadedMesh>[10];
        for (var i = 0; i < tasks.Length; i++)
        {
            tasks[i] = manager.LoadMeshAsync(path);
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
        var path1 = CreateTestMeshFile("mesh1.mesh");
        var path2 = CreateTestMeshFile("mesh2.mesh");

        var mesh1 = await _assetManager.LoadMeshAsync(path1);
        var mesh2 = await _assetManager.LoadMeshAsync(path2);

        Assert.NotSame(mesh1, mesh2);
        Assert.Equal(2, _assetManager.LoadedAssetCount);
    }

    [Fact]
    public async Task LoadMeshAsync_PathNormalization_TreatsSamePathsAsIdentical()
    {
        var path = CreateTestMeshFile("normalize.mesh");
        var path1 = path.ToLower();
        var path2 = path.ToUpper();

        var mesh1 = await _assetManager.LoadMeshAsync(path1);
        var mesh2 = await _assetManager.LoadMeshAsync(path2);

        Assert.Same(mesh1, mesh2);
        Assert.Equal(1, _assetManager.LoadedAssetCount);
    }

    [Fact]
    public void IsLoaded_NullOrEmptyPath_ReturnsFalse()
    {
        Assert.False(_assetManager.IsLoaded(null!));
        Assert.False(_assetManager.IsLoaded(""));
        Assert.False(_assetManager.IsLoaded("   "));
    }

    [Fact]
    public void IsLoading_NullOrEmptyPath_ReturnsFalse()
    {
        Assert.False(_assetManager.IsLoading(null!));
        Assert.False(_assetManager.IsLoading(""));
        Assert.False(_assetManager.IsLoading("   "));
    }

    [Fact]
    public void GetRefCount_NullOrEmptyPath_ReturnsZero()
    {
        Assert.Equal(0, _assetManager.GetRefCount(null!));
        Assert.Equal(0, _assetManager.GetRefCount(""));
        Assert.Equal(0, _assetManager.GetRefCount("   "));
    }

    [Fact]
    public void Unload_NullOrEmptyPath_DoesNotThrow()
    {
        _assetManager.Unload(null!);
        _assetManager.Unload("");
        _assetManager.Unload("   ");
    }

    [Fact]
    public async Task LoadMeshAsync_MultipleConcurrentDifferentAssets_LoadsAllCorrectly()
    {
        var paths = new[]
        {
            CreateTestMeshFile("multi1.mesh"),
            CreateTestMeshFile("multi2.mesh"),
            CreateTestMeshFile("multi3.mesh"),
            CreateTestMeshFile("multi4.mesh"),
            CreateTestMeshFile("multi5.mesh")
        };

        var tasks = paths.Select(p => _assetManager.LoadMeshAsync(p)).ToArray();
        var results = await Task.WhenAll(tasks);

        Assert.Equal(5, _assetManager.LoadedAssetCount);
        Assert.All(results, r => Assert.NotNull(r));
    }

    [Fact]
    public async Task Dispose_WhileLoading_CompletesGracefully()
    {
        var path = CreateTestMeshFile("disposing.mesh");
        var manager = new AssetManager(async p =>
        {
            await Task.Delay(200);
            return new LoadedMesh(CreateTestVertices(), new uint[] { 0, 1, 2 });
        });

        var loadTask = manager.LoadMeshAsync(path);
        await Task.Delay(50);
        manager.Dispose();

        await Task.Delay(300);
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