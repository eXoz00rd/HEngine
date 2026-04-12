using HEngine.Rendering.Assets;
using Silk.NET.DXGI;

namespace HEngine.Rendering.Tests.Assets;

public class TextureLoadResultTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var data = new byte[64];
        var result = new TextureLoadResult(data, 4, 4, 1, Format.FormatR8G8B8A8Unorm, 4, false, "test.png");

        Assert.Equal(data, result.PixelData);
        Assert.Equal(4, result.Width);
        Assert.Equal(4, result.Height);
        Assert.Equal(1, result.MipLevels);
        Assert.Equal(Format.FormatR8G8B8A8Unorm, result.DxgiFormat);
        Assert.Equal(4, result.BytesPerPixel);
        Assert.False(result.IsCompressed);
        Assert.Equal("test.png", result.SourcePath);
    }

    [Fact]
    public void Constructor_ThrowsOnNullData()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new TextureLoadResult(null!, 4, 4, 1, Format.FormatR8G8B8A8Unorm, 4, false, "test.png"));
    }

    [Fact]
    public void Constructor_ThrowsOnZeroWidth()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TextureLoadResult(new byte[16], 0, 4, 1, Format.FormatR8G8B8A8Unorm, 4, false, "test.png"));
    }

    [Fact]
    public void Constructor_ThrowsOnZeroHeight()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TextureLoadResult(new byte[16], 4, 0, 1, Format.FormatR8G8B8A8Unorm, 4, false, "test.png"));
    }

    [Fact]
    public void Constructor_ThrowsOnZeroMipLevels()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TextureLoadResult(new byte[16], 4, 4, 0, Format.FormatR8G8B8A8Unorm, 4, false, "test.png"));
    }

    [Fact]
    public void RowPitch_CalculatedCorrectly()
    {
        var result = new TextureLoadResult(new byte[64], 4, 4, 1, Format.FormatR8G8B8A8Unorm, 4, false, "test.png");
        Assert.Equal(16, result.RowPitch); // 4 * 4 = 16
    }

    [Fact]
    public void RowPitch_IsZeroForCompressed()
    {
        var result = new TextureLoadResult(new byte[8], 4, 4, 1, Format.FormatBC1Unorm, 0, true, "test.dds");
        Assert.Equal(0, result.RowPitch);
    }

    [Fact]
    public void SliceSize_CalculatedCorrectly()
    {
        var result = new TextureLoadResult(new byte[64], 4, 4, 1, Format.FormatR8G8B8A8Unorm, 4, false, "test.png");
        Assert.Equal(64, result.SliceSize); // 4 * 4 * 4 = 64
    }

    [Fact]
    public void SliceSize_UsesDataLengthForCompressed()
    {
        var data = new byte[32];
        var result = new TextureLoadResult(data, 4, 4, 1, Format.FormatBC1Unorm, 0, true, "test.dds");
        Assert.Equal(32, result.SliceSize);
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var result = new TextureLoadResult(new byte[64], 4, 4, 1, Format.FormatR8G8B8A8Unorm, 4, false, "test.png");
        var ex = Record.Exception(() => result.Dispose());
        Assert.Null(ex);
    }

    [Fact]
    public void NullSourcePath_DefaultsToEmpty()
    {
        var result = new TextureLoadResult(new byte[16], 2, 2, 1, Format.FormatR8G8B8A8Unorm, 4, false, null!);
        Assert.Equal(string.Empty, result.SourcePath);
    }
}

