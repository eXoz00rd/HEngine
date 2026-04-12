namespace HEngine.Rendering.Managers;

/// <summary>
/// Defines texture filtering modes for samplers.
/// </summary>
public enum TextureFilterMode
{
    Point,
    Linear,
    Anisotropic
}

/// <summary>
/// Defines texture addressing modes for samplers.
/// </summary>
public enum TextureAddressMode
{
    Wrap,
    Clamp,
    Mirror
}

/// <summary>
/// Immutable descriptor of a sampler configuration.
/// </summary>
public readonly record struct SamplerDescription(
    TextureFilterMode Filter,
    TextureAddressMode AddressU,
    TextureAddressMode AddressV,
    TextureAddressMode AddressW,
    int MaxAnisotropy = 1)
{
    public static SamplerDescription LinearWrap => new(TextureFilterMode.Linear, TextureAddressMode.Wrap, TextureAddressMode.Wrap, TextureAddressMode.Wrap);
    public static SamplerDescription LinearClamp => new(TextureFilterMode.Linear, TextureAddressMode.Clamp, TextureAddressMode.Clamp, TextureAddressMode.Clamp);
    public static SamplerDescription PointWrap => new(TextureFilterMode.Point, TextureAddressMode.Wrap, TextureAddressMode.Wrap, TextureAddressMode.Wrap);
    public static SamplerDescription PointClamp => new(TextureFilterMode.Point, TextureAddressMode.Clamp, TextureAddressMode.Clamp, TextureAddressMode.Clamp);
    public static SamplerDescription AnisotropicWrap(int maxAniso = 16) => new(TextureFilterMode.Anisotropic, TextureAddressMode.Wrap, TextureAddressMode.Wrap, TextureAddressMode.Wrap, maxAniso);
    public static SamplerDescription AnisotropicClamp(int maxAniso = 16) => new(TextureFilterMode.Anisotropic, TextureAddressMode.Clamp, TextureAddressMode.Clamp, TextureAddressMode.Clamp, maxAniso);
}

/// <summary>
/// Manages pre-defined sampler states. Provides sampler descriptors for DX12 static samplers
/// or sampler heap entries. Thread-safe.
/// </summary>
public sealed class SamplerManager
{
    private readonly Dictionary<string, SamplerDescription> _samplers = new();
    private int _maxAnisotropy;

    public int MaxAnisotropy
    {
        get => _maxAnisotropy;
        set => _maxAnisotropy = Math.Clamp(value, 1, 16);
    }

    public int SamplerCount => _samplers.Count;

    public IReadOnlyDictionary<string, SamplerDescription> Samplers => _samplers;

    public SamplerManager(int maxAnisotropy = 16)
    {
        MaxAnisotropy = maxAnisotropy;
        RegisterDefaults();
    }

    public void Register(string name, SamplerDescription description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        _samplers[name] = description;
    }

    public bool TryGet(string name, out SamplerDescription description)
    {
        return _samplers.TryGetValue(name, out description);
    }

    public SamplerDescription Get(string name)
    {
        if (!_samplers.TryGetValue(name, out var desc))
            throw new KeyNotFoundException($"Sampler '{name}' not found.");
        return desc;
    }

    public bool HasSampler(string name) => _samplers.ContainsKey(name);

    /// <summary>
    /// Returns the set of static samplers used in DX12 root signatures.
    /// These cover the most common configurations.
    /// </summary>
    public IReadOnlyList<SamplerDescription> GetStaticSamplers()
    {
        return
        [
            SamplerDescription.LinearWrap,
            SamplerDescription.LinearClamp,
            SamplerDescription.PointWrap,
            SamplerDescription.PointClamp,
            SamplerDescription.AnisotropicWrap(_maxAnisotropy),
            SamplerDescription.AnisotropicClamp(_maxAnisotropy)
        ];
    }

    private void RegisterDefaults()
    {
        Register("LinearWrap", SamplerDescription.LinearWrap);
        Register("LinearClamp", SamplerDescription.LinearClamp);
        Register("PointWrap", SamplerDescription.PointWrap);
        Register("PointClamp", SamplerDescription.PointClamp);
        Register("AnisotropicWrap", SamplerDescription.AnisotropicWrap(_maxAnisotropy));
        Register("AnisotropicClamp", SamplerDescription.AnisotropicClamp(_maxAnisotropy));
        Register("LinearMirror", new SamplerDescription(TextureFilterMode.Linear,
            TextureAddressMode.Mirror, TextureAddressMode.Mirror, TextureAddressMode.Mirror));
    }
}

