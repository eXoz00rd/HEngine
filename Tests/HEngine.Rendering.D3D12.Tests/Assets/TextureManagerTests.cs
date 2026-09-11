using HEngine.Rendering.Managers;
using StbImageWriteSharp;

namespace HEngine.Rendering.Tests.Assets;

public class TextureManagerTests : IDisposable
{
    private readonly string _tempDir;
    private readonly TextureManager _manager;

    public TextureManagerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"HEngine_TexMgr_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _manager = new TextureManager(); // headless mode
    }

    public void Dispose()
    {
        _manager.Dispose();
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    // ─── Default Textures ────────────────────────────────────────────

    [Fact]
    public void DefaultTextures_CreatedOnConstruction()
    {
        Assert.True(_manager.DefaultWhiteTexture >= 0);
        Assert.True(_manager.DefaultNormalTexture >= 0);
        Assert.True(_manager.DefaultBlackTexture >= 0);
    }

    [Fact]
    public void DefaultTextures_AreAllDifferent()
    {
        Assert.NotEqual(_manager.DefaultWhiteTexture, _manager.DefaultNormalTexture);
        Assert.NotEqual(_manager.DefaultWhiteTexture, _manager.DefaultBlackTexture);
        Assert.NotEqual(_manager.DefaultNormalTexture, _manager.DefaultBlackTexture);
    }

    [Fact]
    public void DefaultTextures_AreLoaded()
    {
        Assert.True(_manager.IsTextureLoaded(_manager.DefaultWhiteTexture));
        Assert.True(_manager.IsTextureLoaded(_manager.DefaultNormalTexture));
        Assert.True(_manager.IsTextureLoaded(_manager.DefaultBlackTexture));
    }

    [Fact]
    public void DefaultTextures_HaveRefCount()
    {
        Assert.True(_manager.GetReferenceCount(_manager.DefaultWhiteTexture) > 0);
    }

    [Fact]
    public void LoadedTextureCount_IncludesDefaults()
    {
        Assert.Equal(3, _manager.LoadedTextureCount);
    }

    // ─── Loading ─────────────────────────────────────────────────────

    [Fact]
    public void LoadTexture_ReturnsValidHandle()
    {
        var path = CreateTestPng(4, 4);
        var handle = _manager.LoadTexture(path);

        Assert.True(handle > 0);
        Assert.True(_manager.IsTextureLoaded(handle));
    }

    [Fact]
    public void LoadTexture_IncrementsLoadedCount()
    {
        var path = CreateTestPng(4, 4);
        _manager.LoadTexture(path);

        Assert.Equal(4, _manager.LoadedTextureCount); // 3 defaults + 1
    }

    [Fact]
    public void LoadTexture_SameFileTwice_ReturnsSameHandle()
    {
        var path = CreateTestPng(4, 4);
        var handle1 = _manager.LoadTexture(path);
        var handle2 = _manager.LoadTexture(path);

        Assert.Equal(handle1, handle2);
    }

    [Fact]
    public void LoadTexture_SameFileTwice_IncrementsRefCount()
    {
        var path = CreateTestPng(4, 4);
        var handle = _manager.LoadTexture(path);
        _manager.LoadTexture(path);

        Assert.Equal(2, _manager.GetReferenceCount(handle));
    }

    [Fact]
    public void LoadTexture_DifferentFiles_ReturnDifferentHandles()
    {
        var path1 = CreateTestPng(4, 4);
        var path2 = CreateTestPng(8, 8);
        var handle1 = _manager.LoadTexture(path1);
        var handle2 = _manager.LoadTexture(path2);

        Assert.NotEqual(handle1, handle2);
    }

    [Fact]
    public void LoadTexture_NonExistentFile_ReturnsFallback()
    {
        var handle = _manager.LoadTexture(Path.Combine(_tempDir, "nonexistent.png"));

        Assert.Equal(_manager.DefaultWhiteTexture, handle);
    }

    [Fact]
    public void LoadTexture_NullPath_Throws()
    {
        Assert.Throws<ArgumentException>(() => _manager.LoadTexture(null!));
    }

    [Fact]
    public void LoadTexture_EmptyPath_Throws()
    {
        Assert.Throws<ArgumentException>(() => _manager.LoadTexture(""));
    }

    // ─── Async Loading ───────────────────────────────────────────────

    [Fact]
    public async Task LoadTextureAsync_Works()
    {
        var path = CreateTestPng(4, 4);
        var handle = await _manager.LoadTextureAsync(path);

        Assert.True(handle > 0);
        Assert.True(_manager.IsTextureLoaded(handle));
    }

    [Fact]
    public async Task LoadTextureAsync_NonExistent_ReturnsFallback()
    {
        var handle = await _manager.LoadTextureAsync(Path.Combine(_tempDir, "nope.png"));
        Assert.Equal(_manager.DefaultWhiteTexture, handle);
    }

    // ─── Release / Ref Counting ──────────────────────────────────────

    [Fact]
    public void ReleaseTexture_DecrementsRefCount()
    {
        var path = CreateTestPng(4, 4);
        var handle = _manager.LoadTexture(path);
        _manager.LoadTexture(path); // ref = 2

        _manager.ReleaseTexture(handle);

        Assert.Equal(1, _manager.GetReferenceCount(handle));
        Assert.True(_manager.IsTextureLoaded(handle));
    }

    [Fact]
    public void ReleaseTexture_UnloadsWhenRefCountZero()
    {
        var path = CreateTestPng(4, 4);
        var handle = _manager.LoadTexture(path); // ref = 1

        _manager.ReleaseTexture(handle); // ref = 0

        Assert.False(_manager.IsTextureLoaded(handle));
        Assert.Equal(3, _manager.LoadedTextureCount); // only defaults
    }

    [Fact]
    public void ReleaseTexture_InvalidHandle_DoesNotThrow()
    {
        var ex = Record.Exception(() => _manager.ReleaseTexture(99999));
        Assert.Null(ex);
    }

    [Fact]
    public void ReleaseTexture_DefaultWhite_DoesNotUnload()
    {
        _manager.ReleaseTexture(_manager.DefaultWhiteTexture);
        Assert.True(_manager.IsTextureLoaded(_manager.DefaultWhiteTexture));
    }

    [Fact]
    public void ReleaseTexture_DefaultNormal_DoesNotUnload()
    {
        _manager.ReleaseTexture(_manager.DefaultNormalTexture);
        Assert.True(_manager.IsTextureLoaded(_manager.DefaultNormalTexture));
    }

    [Fact]
    public void ReleaseTexture_DefaultBlack_DoesNotUnload()
    {
        _manager.ReleaseTexture(_manager.DefaultBlackTexture);
        Assert.True(_manager.IsTextureLoaded(_manager.DefaultBlackTexture));
    }

    [Fact]
    public void ReleaseTexture_ReloadAfterRelease_Works()
    {
        var path = CreateTestPng(4, 4);
        var handle1 = _manager.LoadTexture(path);
        _manager.ReleaseTexture(handle1);
        Assert.False(_manager.IsTextureLoaded(handle1));

        var handle2 = _manager.LoadTexture(path);
        Assert.True(_manager.IsTextureLoaded(handle2));
    }

    // ─── Dispose ─────────────────────────────────────────────────────

    [Fact]
    public void Dispose_ClearsAllTextures()
    {
        var path = CreateTestPng(4, 4);
        _manager.LoadTexture(path);

        _manager.Dispose();

        Assert.Equal(0, _manager.LoadedTextureCount);
    }

    [Fact]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        _manager.Dispose();
        var ex = Record.Exception(() => _manager.Dispose());
        Assert.Null(ex);
    }

    [Fact]
    public void LoadTexture_AfterDispose_Throws()
    {
        _manager.Dispose();
        Assert.Throws<ObjectDisposedException>(() => _manager.LoadTexture(CreateTestPng(2, 2)));
    }

    // ─── IsTextureLoaded / GetReferenceCount edge cases ──────────────

    [Fact]
    public void IsTextureLoaded_InvalidHandle_ReturnsFalse()
    {
        Assert.False(_manager.IsTextureLoaded(-1));
        Assert.False(_manager.IsTextureLoaded(99999));
    }

    [Fact]
    public void GetReferenceCount_InvalidHandle_ReturnsZero()
    {
        Assert.Equal(0, _manager.GetReferenceCount(-1));
        Assert.Equal(0, _manager.GetReferenceCount(99999));
    }

    // ─── Helpers ─────────────────────────────────────────────────────

    private string CreateTestPng(int width, int height)
    {
        var path = Path.Combine(_tempDir, $"tex_{width}x{height}_{Guid.NewGuid():N}.png");
        var pixels = new byte[width * height * 4];
        Array.Fill(pixels, (byte)128);

        using var stream = File.OpenWrite(path);
        var writer = new ImageWriter();
        writer.WritePng(pixels, width, height, ColorComponents.RedGreenBlueAlpha, stream);

        return path;
    }
}

