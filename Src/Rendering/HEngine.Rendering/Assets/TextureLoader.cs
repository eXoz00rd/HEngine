using Silk.NET.DXGI;
using StbImageSharp;

namespace HEngine.Rendering.Assets;

/// <summary>
/// Loads texture files from disk into CPU-side TextureLoadResult.
/// Supports PNG, JPG, BMP, TGA (via StbImageSharp) and DDS (custom parser).
/// </summary>
public sealed class TextureLoader
{
    /// <summary>
    /// Synchronously loads a texture from the given file path.
    /// </summary>
    public TextureLoadResult Load(string filePath)
    {
        ValidatePath(filePath);

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".dds" => LoadDds(filePath),
            ".png" or ".jpg" or ".jpeg" or ".bmp" or ".tga" => LoadStbImage(filePath),
            _ => throw new NotSupportedException($"Unsupported texture format: '{extension}'. Supported: .png, .jpg, .bmp, .tga, .dds")
        };
    }

    /// <summary>
    /// Asynchronously loads a texture from the given file path.
    /// </summary>
    public async Task<TextureLoadResult> LoadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ValidatePath(filePath);
        return await Task.Run(() => Load(filePath), cancellationToken);
    }

    private static TextureLoadResult LoadStbImage(string filePath)
    {
        byte[] fileBytes = File.ReadAllBytes(filePath);

        var image = ImageResult.FromMemory(fileBytes, ColorComponents.RedGreenBlueAlpha);

        return new TextureLoadResult(
            pixelData: image.Data,
            width: image.Width,
            height: image.Height,
            mipLevels: 1,
            dxgiFormat: Format.FormatR8G8B8A8Unorm,
            bytesPerPixel: 4,
            isCompressed: false,
            sourcePath: filePath);
    }

    private static TextureLoadResult LoadDds(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        using var reader = new BinaryReader(stream);

        // DDS magic number
        uint magic = reader.ReadUInt32();
        if (magic != 0x20534444) // "DDS "
            throw new InvalidDataException($"Invalid DDS file: '{filePath}'. Bad magic number.");

        // DDS_HEADER (124 bytes)
        uint headerSize = reader.ReadUInt32();
        if (headerSize != 124)
            throw new InvalidDataException($"Invalid DDS header size: {headerSize}");

        uint flags = reader.ReadUInt32();
        uint height = reader.ReadUInt32();
        uint width = reader.ReadUInt32();
        uint pitchOrLinearSize = reader.ReadUInt32();
        uint depth = reader.ReadUInt32();
        uint mipMapCount = reader.ReadUInt32();

        // Reserved (11 × uint)
        reader.ReadBytes(44);

        // DDS_PIXELFORMAT (32 bytes)
        uint pfSize = reader.ReadUInt32();
        uint pfFlags = reader.ReadUInt32();
        uint fourCC = reader.ReadUInt32();
        uint rgbBitCount = reader.ReadUInt32();
        uint rBitMask = reader.ReadUInt32();
        uint gBitMask = reader.ReadUInt32();
        uint bBitMask = reader.ReadUInt32();
        uint aBitMask = reader.ReadUInt32();

        // Caps
        uint caps = reader.ReadUInt32();
        uint caps2 = reader.ReadUInt32();
        reader.ReadBytes(12); // caps3, caps4, reserved2

        if (mipMapCount == 0)
            mipMapCount = 1;

        // Determine format from FourCC
        var (dxgiFormat, blockSize, isCompressed) = DecodeDdsFormat(fourCC, pfFlags, rgbBitCount);

        // Read all remaining data as pixel data
        var dataSize = (int)(stream.Length - stream.Position);
        byte[] pixelData = reader.ReadBytes(dataSize);

        int bpp = isCompressed ? 0 : (int)(rgbBitCount / 8);
        if (!isCompressed && bpp == 0) bpp = 4;

        return new TextureLoadResult(
            pixelData: pixelData,
            width: (int)width,
            height: (int)height,
            mipLevels: (int)mipMapCount,
            dxgiFormat: dxgiFormat,
            bytesPerPixel: bpp,
            isCompressed: isCompressed,
            sourcePath: filePath);
    }

    private static (Format format, int blockSize, bool isCompressed) DecodeDdsFormat(
        uint fourCC, uint pfFlags, uint rgbBitCount)
    {
        const uint DDPF_FOURCC = 0x4;
        const uint DDPF_RGB = 0x40;

        if ((pfFlags & DDPF_FOURCC) != 0)
        {
            return fourCC switch
            {
                0x31545844 => (Format.FormatBC1Unorm, 8, true),   // "DXT1"
                0x33545844 => (Format.FormatBC2Unorm, 16, true),  // "DXT3"
                0x35545844 => (Format.FormatBC3Unorm, 16, true),  // "DXT5"
                0x55344342 => (Format.FormatBC4Unorm, 8, true),   // "BC4U"
                0x53344342 => (Format.FormatBC4Unorm, 8, true),   // "BC4S" (fallback to Unorm)
                0x55354342 => (Format.FormatBC5Unorm, 16, true),  // "BC5U"
                0x53354342 => (Format.FormatBC5Unorm, 16, true),  // "BC5S" (fallback to Unorm)
                _ => throw new NotSupportedException($"Unsupported DDS FourCC: 0x{fourCC:X8}")
            };
        }

        if ((pfFlags & DDPF_RGB) != 0)
        {
            return rgbBitCount switch
            {
                32 => (Format.FormatR8G8B8A8Unorm, 0, false),
                24 => (Format.FormatR8G8B8A8Unorm, 0, false), // Will need expansion
                _ => throw new NotSupportedException($"Unsupported DDS RGB bit count: {rgbBitCount}")
            };
        }

        throw new NotSupportedException("Unsupported DDS pixel format flags.");
    }

    private static void ValidatePath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Texture file not found: '{filePath}'", filePath);
    }
}


