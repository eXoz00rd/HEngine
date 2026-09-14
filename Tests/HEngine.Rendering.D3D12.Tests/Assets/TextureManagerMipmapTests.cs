using HEngine.Rendering.Managers;

namespace HEngine.Rendering.Tests.Assets;

public class TextureManagerMipmapTests
{
    [Theory]
    [InlineData(1, 1, 1)]
    [InlineData(2, 2, 2)]
    [InlineData(4, 4, 3)]
    [InlineData(8, 8, 4)]
    [InlineData(16, 16, 5)]
    [InlineData(256, 256, 9)]
    [InlineData(1024, 1024, 11)]
    public void CalculateMipLevels_PowerOfTwo(int width, int height, int expected)
    {
        Assert.Equal(expected, TextureManager.CalculateMipLevels(width, height));
    }

    [Theory]
    [InlineData(3, 3, 2)]
    [InlineData(5, 5, 3)]
    [InlineData(7, 7, 3)]
    [InlineData(100, 100, 7)]
    [InlineData(1920, 1080, 11)]
    public void CalculateMipLevels_NonPowerOfTwo(int width, int height, int expected)
    {
        Assert.Equal(expected, TextureManager.CalculateMipLevels(width, height));
    }

    [Theory]
    [InlineData(512, 1, 10)]
    [InlineData(1, 512, 10)]
    public void CalculateMipLevels_AsymmetricDimensions(int width, int height, int expected)
    {
        Assert.Equal(expected, TextureManager.CalculateMipLevels(width, height));
    }

    [Fact]
    public void GenerateMipmapsCpu_1x1_SingleMip()
    {
        var pixels = new byte[] { 255, 0, 0, 255 }; // red
        var result = TextureManager.GenerateMipmapsCpu(pixels, 1, 1, 4, out var offsets, out var levels);

        Assert.Equal(1, levels);
        Assert.Single(offsets);
        Assert.Equal(0, offsets[0]);
        Assert.Equal(4, result.Length);
        Assert.Equal(255, result[0]); // R
    }

    [Fact]
    public void GenerateMipmapsCpu_2x2_TwoMips()
    {
        // 2x2 uniform red pixels
        var pixels = new byte[]
        {
            255, 0, 0, 255,  255, 0, 0, 255,
            255, 0, 0, 255,  255, 0, 0, 255,
        };

        var result = TextureManager.GenerateMipmapsCpu(pixels, 2, 2, 4, out var offsets, out var levels);

        Assert.Equal(2, levels);
        Assert.Equal(2, offsets.Length);
        Assert.Equal(0, offsets[0]);
        Assert.Equal(16, offsets[1]); // 2*2*4 = 16

        // Total = mip0 (16) + mip1 (4) = 20 bytes
        Assert.Equal(20, result.Length);

        // Mip 1 should be 1x1 red (averaged from 4 red pixels)
        Assert.Equal(255, result[16]); // R
        Assert.Equal(0, result[17]);   // G
        Assert.Equal(0, result[18]);   // B
        Assert.Equal(255, result[19]); // A
    }

    [Fact]
    public void GenerateMipmapsCpu_4x4_ThreeMips()
    {
        // 4x4 white pixels
        var pixels = new byte[4 * 4 * 4];
        Array.Fill(pixels, (byte)200);

        var result = TextureManager.GenerateMipmapsCpu(pixels, 4, 4, 4, out var offsets, out var levels);

        Assert.Equal(3, levels); // 4x4 → 2x2 → 1x1
        Assert.Equal(3, offsets.Length);

        // mip0: 4*4*4 = 64
        Assert.Equal(0, offsets[0]);
        // mip1: 2*2*4 = 16, offset = 64
        Assert.Equal(64, offsets[1]);
        // mip2: 1*1*4 = 4, offset = 80
        Assert.Equal(80, offsets[2]);

        // Total = 64 + 16 + 4 = 84
        Assert.Equal(84, result.Length);

        // Mip levels should be averaged from parent — uniform 200 stays 200
        Assert.Equal(200, result[64]); // mip1 first pixel R
        Assert.Equal(200, result[80]); // mip2 first pixel R
    }

    [Fact]
    public void GenerateMipmapsCpu_2x2_AveragesCorrectly()
    {
        // 2x2 pixels: (0,0,0,255), (100,0,0,255), (200,0,0,255), (100,0,0,255)
        // Average R = (0+100+200+100)/4 = 100
        var pixels = new byte[]
        {
            0, 0, 0, 255,    100, 0, 0, 255,
            200, 0, 0, 255,  100, 0, 0, 255,
        };

        var result = TextureManager.GenerateMipmapsCpu(pixels, 2, 2, 4, out _, out _);

        // Mip1 is at offset 16
        Assert.Equal(100, result[16]); // R averaged
    }

    [Fact]
    public void GenerateMipmapsCpu_Mip0_MatchesSource()
    {
        var pixels = new byte[] { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110, 120, 130, 140, 150, 160 };
        var result = TextureManager.GenerateMipmapsCpu(pixels, 2, 2, 4, out var offsets, out _);

        for (int i = 0; i < pixels.Length; i++)
            Assert.Equal(pixels[i], result[offsets[0] + i]);
    }

    [Fact]
    public void GenerateMipmapsCpu_8x8_CorrectLevelCount()
    {
        var pixels = new byte[8 * 8 * 4];
        Array.Fill(pixels, (byte)128);

        TextureManager.GenerateMipmapsCpu(pixels, 8, 8, 4, out var offsets, out var levels);

        Assert.Equal(4, levels); // 8x8 → 4x4 → 2x2 → 1x1
        Assert.Equal(4, offsets.Length);
    }
}

