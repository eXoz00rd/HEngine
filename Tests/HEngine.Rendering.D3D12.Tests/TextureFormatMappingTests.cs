using HEngine.Rendering.Assets;
using HEngine.Rendering.Direct3D12;
using Silk.NET.DXGI;

namespace HEngine.Rendering.D3D12.Tests;

public class TextureFormatMappingTests
{
    [Theory(DisplayName = "Each neutral texture format maps to its DXGI counterpart")]
    [InlineData(TextureFormat.R8G8B8A8Unorm, Format.FormatR8G8B8A8Unorm)]
    [InlineData(TextureFormat.BC1Unorm, Format.FormatBC1Unorm)]
    [InlineData(TextureFormat.BC2Unorm, Format.FormatBC2Unorm)]
    [InlineData(TextureFormat.BC3Unorm, Format.FormatBC3Unorm)]
    [InlineData(TextureFormat.BC4Unorm, Format.FormatBC4Unorm)]
    [InlineData(TextureFormat.BC5Unorm, Format.FormatBC5Unorm)]
    public void ToDxgi_MapsEachFormat(TextureFormat format, Format expected)
    {
        Assert.Equal(expected, format.ToDxgi());
    }

    [Fact(DisplayName = "Unknown maps to the DXGI unknown format rather than a real one")]
    public void ToDxgi_MapsUnknown()
    {
        Assert.Equal(Format.FormatUnknown, TextureFormat.Unknown.ToDxgi());
    }

    [Fact(DisplayName = "Every declared texture format has a mapping, so a new one cannot reach the GPU as Unknown")]
    public void ToDxgi_CoversEveryDeclaredFormat()
    {
        var unmapped = Enum.GetValues<TextureFormat>()
            .Where(format => format != TextureFormat.Unknown)
            .Where(format => format.ToDxgi() == Format.FormatUnknown)
            .ToList();

        Assert.Empty(unmapped);
    }

    [Fact(DisplayName = "Distinct texture formats do not collide on one DXGI format")]
    public void ToDxgi_IsInjective()
    {
        var mapped = Enum.GetValues<TextureFormat>()
            .Where(format => format != TextureFormat.Unknown)
            .Select(format => format.ToDxgi())
            .ToList();

        Assert.Equal(mapped.Count, mapped.Distinct().Count());
    }
}
