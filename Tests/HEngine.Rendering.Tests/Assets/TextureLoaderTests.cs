using HEngine.Rendering.Assets;
using Silk.NET.DXGI;
using StbImageWriteSharp;

namespace HEngine.Rendering.Tests.Assets;

public class TextureLoaderTests : IDisposable
{
    private readonly string _tempDir;
    private readonly TextureLoader _loader;

    public TextureLoaderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"HEngine_Tests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _loader = new TextureLoader();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    // ─── PNG / JPG Loading ───────────────────────────────────────────

    [Fact]
    public void Load_Png_ReturnsCorrectDimensions()
    {
        var path = CreateTestPng(8, 8);
        using var result = _loader.Load(path);

        Assert.Equal(8, result.Width);
        Assert.Equal(8, result.Height);
        Assert.Equal(Format.FormatR8G8B8A8Unorm, result.DxgiFormat);
        Assert.Equal(4, result.BytesPerPixel);
        Assert.False(result.IsCompressed);
        Assert.Equal(1, result.MipLevels);
    }

    [Fact]
    public void Load_Png_PixelDataHasCorrectLength()
    {
        var path = CreateTestPng(4, 4);
        using var result = _loader.Load(path);

        Assert.Equal(4 * 4 * 4, result.PixelData.Length); // 4×4 RGBA
    }

    [Fact]
    public void Load_Png_PixelDataIsNotAllZero()
    {
        var path = CreateTestPng(2, 2, 255, 0, 0, 255); // red
        using var result = _loader.Load(path);

        Assert.True(result.PixelData.Any(b => b != 0));
    }

    [Fact]
    public void Load_Png_LargeTexture()
    {
        var path = CreateTestPng(256, 256);
        using var result = _loader.Load(path);

        Assert.Equal(256, result.Width);
        Assert.Equal(256, result.Height);
        Assert.Equal(256 * 256 * 4, result.PixelData.Length);
    }

    [Fact]
    public void Load_Png_1x1Texture()
    {
        var path = CreateTestPng(1, 1);
        using var result = _loader.Load(path);

        Assert.Equal(1, result.Width);
        Assert.Equal(1, result.Height);
        Assert.Equal(4, result.PixelData.Length);
    }

    [Fact]
    public void Load_Png_SourcePathIsSet()
    {
        var path = CreateTestPng(4, 4);
        using var result = _loader.Load(path);

        Assert.Equal(path, result.SourcePath);
    }

    [Fact]
    public void Load_Bmp_Works()
    {
        var path = CreateTestBmp(4, 4);
        using var result = _loader.Load(path);

        Assert.Equal(4, result.Width);
        Assert.Equal(4, result.Height);
        Assert.Equal(Format.FormatR8G8B8A8Unorm, result.DxgiFormat);
    }

    // ─── Async Loading ───────────────────────────────────────────────

    [Fact]
    public async Task LoadAsync_Png_Works()
    {
        var path = CreateTestPng(8, 8);
        using var result = await _loader.LoadAsync(path);

        Assert.Equal(8, result.Width);
        Assert.Equal(8, result.Height);
    }

    [Fact]
    public async Task LoadAsync_CanBeCancelled()
    {
        var path = CreateTestPng(4, 4);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _loader.LoadAsync(path, cts.Token));
    }

    // ─── Error Handling ──────────────────────────────────────────────

    [Fact]
    public void Load_ThrowsOnNullPath()
    {
        Assert.Throws<ArgumentException>(() => _loader.Load(null!));
    }

    [Fact]
    public void Load_ThrowsOnEmptyPath()
    {
        Assert.Throws<ArgumentException>(() => _loader.Load(""));
    }

    [Fact]
    public void Load_ThrowsOnWhitespacePath()
    {
        Assert.Throws<ArgumentException>(() => _loader.Load("   "));
    }

    [Fact]
    public void Load_ThrowsOnFileNotFound()
    {
        Assert.Throws<FileNotFoundException>(() => _loader.Load(Path.Combine(_tempDir, "nonexistent.png")));
    }

    [Fact]
    public void Load_ThrowsOnUnsupportedFormat()
    {
        var path = Path.Combine(_tempDir, "test.xyz");
        File.WriteAllBytes(path, new byte[16]);

        Assert.Throws<NotSupportedException>(() => _loader.Load(path));
    }

    [Fact]
    public void Load_DdsInvalidMagic_Throws()
    {
        var path = Path.Combine(_tempDir, "bad.dds");
        File.WriteAllBytes(path, new byte[256]); // zeroed out — invalid magic

        Assert.Throws<InvalidDataException>(() => _loader.Load(path));
    }

    // ─── DDS Loading ─────────────────────────────────────────────────

    [Fact]
    public void Load_Dds_ValidUncompressed_Works()
    {
        var path = CreateTestDds(4, 4, compressed: false);
        using var result = _loader.Load(path);

        Assert.Equal(4, result.Width);
        Assert.Equal(4, result.Height);
        Assert.Equal(Format.FormatR8G8B8A8Unorm, result.DxgiFormat);
        Assert.False(result.IsCompressed);
    }

    [Fact]
    public void Load_Dds_BC1Compressed_Works()
    {
        var path = CreateTestDds(4, 4, compressed: true);
        using var result = _loader.Load(path);

        Assert.Equal(4, result.Width);
        Assert.Equal(4, result.Height);
        Assert.Equal(Format.FormatBC1Unorm, result.DxgiFormat);
        Assert.True(result.IsCompressed);
    }

    [Fact]
    public void Load_Dds_MipLevels()
    {
        var path = CreateTestDds(8, 8, compressed: false, mipCount: 4);
        using var result = _loader.Load(path);

        Assert.Equal(4, result.MipLevels);
    }

    // ─── Test Helpers ────────────────────────────────────────────────

    private string CreateTestPng(int width, int height, byte r = 128, byte g = 128, byte b = 128, byte a = 255)
    {
        var path = Path.Combine(_tempDir, $"test_{width}x{height}_{Guid.NewGuid():N}.png");
        var pixels = new byte[width * height * 4];
        for (int i = 0; i < pixels.Length; i += 4)
        {
            pixels[i] = r;
            pixels[i + 1] = g;
            pixels[i + 2] = b;
            pixels[i + 3] = a;
        }

        using var stream = File.OpenWrite(path);
        var writer = new ImageWriter();
        writer.WritePng(pixels, width, height, ColorComponents.RedGreenBlueAlpha, stream);

        return path;
    }

    private string CreateTestBmp(int width, int height)
    {
        var path = Path.Combine(_tempDir, $"test_{width}x{height}_{Guid.NewGuid():N}.bmp");
        var pixels = new byte[width * height * 4];
        Array.Fill(pixels, (byte)200);

        using var stream = File.OpenWrite(path);
        var writer = new ImageWriter();
        writer.WriteBmp(pixels, width, height, ColorComponents.RedGreenBlueAlpha, stream);

        return path;
    }

    private string CreateTestDds(int width, int height, bool compressed, int mipCount = 1)
    {
        var path = Path.Combine(_tempDir, $"test_{width}x{height}_{Guid.NewGuid():N}.dds");
        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream);

        // Magic
        writer.Write(0x20534444u); // "DDS "

        // Header (124 bytes)
        writer.Write(124u); // size
        uint flags = 0x1 | 0x2 | 0x4 | 0x1000; // CAPS | HEIGHT | WIDTH | PIXELFORMAT
        if (mipCount > 1) flags |= 0x20000; // MIPMAPCOUNT
        writer.Write(flags);
        writer.Write((uint)height);
        writer.Write((uint)width);

        int dataSize;
        if (compressed)
        {
            // BC1: 8 bytes per 4x4 block
            int blocksX = Math.Max(1, (width + 3) / 4);
            int blocksY = Math.Max(1, (height + 3) / 4);
            dataSize = blocksX * blocksY * 8;
            writer.Write((uint)dataSize); // pitchOrLinearSize
        }
        else
        {
            dataSize = width * height * 4;
            writer.Write((uint)(width * 4)); // pitch
        }

        writer.Write(0u); // depth
        writer.Write((uint)mipCount); // mipMapCount
        for (int i = 0; i < 11; i++) writer.Write(0u); // reserved

        // Pixel format (32 bytes)
        writer.Write(32u); // pfSize
        if (compressed)
        {
            writer.Write(0x4u); // DDPF_FOURCC
            writer.Write(0x31545844u); // "DXT1"
            writer.Write(0u); // rgbBitCount
        }
        else
        {
            writer.Write(0x40u); // DDPF_RGB
            writer.Write(0u); // fourCC
            writer.Write(32u); // rgbBitCount
        }

        writer.Write(0x00FF0000u); // rBitMask
        writer.Write(0x0000FF00u); // gBitMask
        writer.Write(0x000000FFu); // bBitMask
        writer.Write(0xFF000000u); // aBitMask

        // Caps
        writer.Write(0x1000u); // caps = TEXTURE
        writer.Write(0u); // caps2
        writer.Write(0u); // caps3
        writer.Write(0u); // caps4
        writer.Write(0u); // reserved2

        // Pixel data
        var pixels = new byte[dataSize];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = (byte)(i % 256);
        writer.Write(pixels);

        return path;
    }
}

