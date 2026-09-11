using HEngine.Rendering.Assets;
using Silk.NET.DXGI;

namespace HEngine.Rendering.Direct3D12;

public static class TextureFormatMapping
{
    public static Format ToDxgi(this TextureFormat format) => format switch
    {
        TextureFormat.R8G8B8A8Unorm => Format.FormatR8G8B8A8Unorm,
        TextureFormat.BC1Unorm => Format.FormatBC1Unorm,
        TextureFormat.BC2Unorm => Format.FormatBC2Unorm,
        TextureFormat.BC3Unorm => Format.FormatBC3Unorm,
        TextureFormat.BC4Unorm => Format.FormatBC4Unorm,
        TextureFormat.BC5Unorm => Format.FormatBC5Unorm,
        _ => Format.FormatUnknown
    };
}
