using Silk.NET.DXGI;

namespace HEngine.Rendering.Assets;

/// <summary>
/// Result of loading a texture from disk. Holds raw pixel data and metadata.
/// This is a CPU-side representation — no GPU resources allocated yet.
/// </summary>
public sealed class TextureLoadResult : IDisposable
{
    public byte[] PixelData { get; }
    public int Width { get; }
    public int Height { get; }
    public int MipLevels { get; }
    public Format DxgiFormat { get; }
    public int BytesPerPixel { get; }
    public bool IsCompressed { get; }
    public string SourcePath { get; }

    public TextureLoadResult(
        byte[] pixelData,
        int width,
        int height,
        int mipLevels,
        Format dxgiFormat,
        int bytesPerPixel,
        bool isCompressed,
        string sourcePath)
    {
        PixelData = pixelData ?? throw new ArgumentNullException(nameof(pixelData));
        Width = width > 0 ? width : throw new ArgumentOutOfRangeException(nameof(width));
        Height = height > 0 ? height : throw new ArgumentOutOfRangeException(nameof(height));
        MipLevels = mipLevels > 0 ? mipLevels : throw new ArgumentOutOfRangeException(nameof(mipLevels));
        DxgiFormat = dxgiFormat;
        BytesPerPixel = bytesPerPixel;
        IsCompressed = isCompressed;
        SourcePath = sourcePath ?? string.Empty;
    }

    public int RowPitch => IsCompressed ? 0 : Width * BytesPerPixel;
    public int SliceSize => IsCompressed ? PixelData.Length : Width * Height * BytesPerPixel;

    public void Dispose()
    {
        // Pixel data is managed, GC handles it. 
        // Placeholder for future native memory usage.
    }
}

