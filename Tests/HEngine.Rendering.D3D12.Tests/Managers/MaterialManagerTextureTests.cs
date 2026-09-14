using HEngine.Core.Rendering.Contracts;
using HEngine.Rendering.Data;
using HEngine.Rendering.Enums;
using HEngine.Rendering.Managers;

namespace HEngine.Rendering.Tests.Managers;

public class MaterialManagerTextureTests : IDisposable
{
    private readonly MaterialManager _materialManager;
    private readonly TextureManager _textureManager;
    private readonly string _tempDir;

    public MaterialManagerTextureTests()
    {
        _materialManager = new MaterialManager();
        _textureManager = new TextureManager();
        _tempDir = Path.Combine(Path.GetTempPath(), $"HEngine_MatMgr_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        _textureManager.Dispose();
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void ResolveTextureBindings_WithNoTextures_ReturnsEmptyBindings()
    {
        var mat = new Material();
        _materialManager.Register("test", mat);

        var bindings = _materialManager.ResolveTextureBindings("test", _textureManager);

        Assert.NotNull(bindings);
        Assert.Equal(0, bindings.BoundCount);
    }

    [Fact]
    public void ResolveTextureBindings_WithDiffuseTexture_ResolvesDiffuseSlot()
    {
        var pngPath = CreateTestPng("diffuse.png");
        var mat = new Material();
        mat.DiffuseTexture = pngPath;
        _materialManager.Register("textured", mat);

        var bindings = _materialManager.ResolveTextureBindings("textured", _textureManager);

        Assert.True(bindings.HasBinding(TextureSlot.DiffuseMap));
        Assert.True(bindings.TryGetBinding(TextureSlot.DiffuseMap, out var binding));
        Assert.True(binding.TextureHandle > 0);
    }

    [Fact]
    public void ResolveTextureBindings_CachesResult()
    {
        var mat = new Material();
        _materialManager.Register("cached", mat);

        var bindings1 = _materialManager.ResolveTextureBindings("cached", _textureManager);
        var bindings2 = _materialManager.ResolveTextureBindings("cached", _textureManager);

        Assert.Same(bindings1, bindings2);
    }

    [Fact]
    public void ResolveTextureBindings_ThrowsOnUnknownMaterial()
    {
        Assert.Throws<KeyNotFoundException>(() =>
            _materialManager.ResolveTextureBindings("nonexistent", _textureManager));
    }

    [Fact]
    public void ResolveTextureBindings_ThrowsOnNullManager()
    {
        _materialManager.Register("test", new Material());
        Assert.Throws<ArgumentNullException>(() =>
            _materialManager.ResolveTextureBindings("test", null!));
    }

    [Fact]
    public void GetTextureBindings_ReturnsNull_IfNotResolved()
    {
        _materialManager.Register("unresolvedmat", new Material());
        Assert.Null(_materialManager.GetTextureBindings("unresolvedmat"));
    }

    [Fact]
    public void GetTextureBindings_ReturnsBindings_AfterResolve()
    {
        _materialManager.Register("resolvedmat", new Material());
        _materialManager.ResolveTextureBindings("resolvedmat", _textureManager);

        Assert.NotNull(_materialManager.GetTextureBindings("resolvedmat"));
    }

    [Fact]
    public void GetTextureHandleForSlot_ReturnsFallbackDiffuse_WhenNoBinding()
    {
        _materialManager.Register("empty", new Material());

        var handle = _materialManager.GetTextureHandleForSlot("empty", TextureSlot.DiffuseMap, _textureManager);
        Assert.Equal(_textureManager.DefaultWhiteTexture, handle);
    }

    [Fact]
    public void GetTextureHandleForSlot_ReturnsFallbackNormal_WhenNoBinding()
    {
        _materialManager.Register("empty2", new Material());

        var handle = _materialManager.GetTextureHandleForSlot("empty2", TextureSlot.NormalMap, _textureManager);
        Assert.Equal(_textureManager.DefaultNormalTexture, handle);
    }

    [Fact]
    public void GetTextureHandleForSlot_ReturnsFallbackBlack_ForOtherSlots()
    {
        _materialManager.Register("empty3", new Material());

        var handle = _materialManager.GetTextureHandleForSlot("empty3", TextureSlot.MetallicRoughnessMap, _textureManager);
        Assert.Equal(_textureManager.DefaultBlackTexture, handle);
    }

    [Fact]
    public void GetTextureHandleForSlot_ReturnsBoundTexture_WhenAvailable()
    {
        var pngPath = CreateTestPng("albedo.png");
        var mat = new Material();
        mat.DiffuseTexture = pngPath;
        _materialManager.Register("with_tex", mat);

        var handle = _materialManager.GetTextureHandleForSlot("with_tex", TextureSlot.DiffuseMap, _textureManager);

        Assert.NotEqual(_textureManager.DefaultWhiteTexture, handle);
        Assert.True(handle > 0);
    }

    [Fact]
    public void InvalidateTextureBindings_ClearsCache()
    {
        _materialManager.Register("invalidate_test", new Material());
        _materialManager.ResolveTextureBindings("invalidate_test", _textureManager);

        Assert.NotNull(_materialManager.GetTextureBindings("invalidate_test"));

        _materialManager.InvalidateTextureBindings("invalidate_test");

        Assert.Null(_materialManager.GetTextureBindings("invalidate_test"));
    }

    [Fact]
    public void Remove_AlsoRemovesTextureBindings()
    {
        _materialManager.Register("remove_test", new Material());
        _materialManager.ResolveTextureBindings("remove_test", _textureManager);

        _materialManager.Remove("remove_test");

        Assert.Null(_materialManager.GetTextureBindings("remove_test"));
    }

    [Fact]
    public void Clear_AlsoRemovesTextureBindings()
    {
        _materialManager.Register("clear_test", new Material());
        _materialManager.ResolveTextureBindings("clear_test", _textureManager);

        _materialManager.Clear();

        Assert.Null(_materialManager.GetTextureBindings("clear_test"));
        Assert.Equal(0, _materialManager.Count);
    }

    private string CreateTestPng(string fileName)
    {
        var path = Path.Combine(_tempDir, fileName);
        var pixels = new byte[4 * 4 * 4];
        Array.Fill(pixels, (byte)128);

        using var stream = File.OpenWrite(path);
        var writer = new StbImageWriteSharp.ImageWriter();
        writer.WritePng(pixels, 4, 4, StbImageWriteSharp.ColorComponents.RedGreenBlueAlpha, stream);

        return path;
    }
}

