namespace HEngine.Rendering.PostProcessing;

/// <summary>
/// Represents the (width, height) dimensions of a single level in the bloom downsample chain.
/// </summary>
public readonly struct BloomMipLevel
{
    public BloomMipLevel(int width, int height, int level)
    {
        Width = width;
        Height = height;
        Level = level;
    }

    public int Width { get; }
    public int Height { get; }
    public int Level { get; }
}

/// <summary>
/// Multi-pass bloom effect: brightness extraction → downsample chain → Gaussian blur → upsample additive composite.
/// Order: 600 (applied after main render, before tone mapping).
/// </summary>
public sealed class BloomEffect : IPostProcessEffect
{
    public const int MaxMipLevels = 5;

    public string Name => "Bloom";
    public bool IsEnabled { get; set; } = true;
    public int Order => 600;

    public float Threshold { get; set; } = 1.0f;
    public float Intensity { get; set; } = 1.0f;
    public float Radius { get; set; } = 1.0f;
    public int MipLevels { get; set; } = 5;

    public void Execute(IPostProcessCommandContext context)
    {
        var mips = ComputeDownsampleChain(context.Width, context.Height, MipLevels);

        context.SetConstantFloat("BloomThreshold", Threshold);
        context.SetConstantFloat("BloomIntensity", Intensity);
        context.SetConstantFloat("BloomRadius", Radius);
        context.SetConstantInt("BloomMipLevels", mips.Length);

        context.DrawFullscreenTriangle();
    }

    /// <summary>
    /// Computes the width and height of each downsample mip level.
    /// Each level is half the size of the previous (minimum 1x1).
    /// </summary>
    public static BloomMipLevel[] ComputeDownsampleChain(int width, int height, int levels)
    {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (levels <= 0) throw new ArgumentOutOfRangeException(nameof(levels));

        levels = Math.Min(levels, MaxMipLevels);

        var chain = new BloomMipLevel[levels];
        var w = width;
        var h = height;

        for (var i = 0; i < levels; i++)
        {
            chain[i] = new BloomMipLevel(w, h, i);
            w = Math.Max(1, w / 2);
            h = Math.Max(1, h / 2);
        }

        return chain;
    }
}

