using HEngine.Rendering.Managers;

namespace HEngine.Rendering.Tests.Managers;

public class SamplerManagerTests
{
    [Fact]
    public void Constructor_RegistersDefaultSamplers()
    {
        var manager = new SamplerManager();

        Assert.True(manager.HasSampler("LinearWrap"));
        Assert.True(manager.HasSampler("LinearClamp"));
        Assert.True(manager.HasSampler("PointWrap"));
        Assert.True(manager.HasSampler("PointClamp"));
        Assert.True(manager.HasSampler("AnisotropicWrap"));
        Assert.True(manager.HasSampler("AnisotropicClamp"));
        Assert.True(manager.HasSampler("LinearMirror"));
    }

    [Fact]
    public void Constructor_DefaultSamplerCount()
    {
        var manager = new SamplerManager();
        Assert.Equal(7, manager.SamplerCount);
    }

    [Fact]
    public void MaxAnisotropy_DefaultIs16()
    {
        var manager = new SamplerManager();
        Assert.Equal(16, manager.MaxAnisotropy);
    }

    [Fact]
    public void MaxAnisotropy_ClampedTo1_16()
    {
        var manager = new SamplerManager(0);
        Assert.Equal(1, manager.MaxAnisotropy);

        manager.MaxAnisotropy = 32;
        Assert.Equal(16, manager.MaxAnisotropy);

        manager.MaxAnisotropy = 8;
        Assert.Equal(8, manager.MaxAnisotropy);
    }

    [Fact]
    public void Constructor_CustomAnisotropy()
    {
        var manager = new SamplerManager(4);
        Assert.Equal(4, manager.MaxAnisotropy);
    }

    [Fact]
    public void Get_ReturnsCorrectSampler()
    {
        var manager = new SamplerManager();
        var sampler = manager.Get("LinearWrap");

        Assert.Equal(TextureFilterMode.Linear, sampler.Filter);
        Assert.Equal(TextureAddressMode.Wrap, sampler.AddressU);
        Assert.Equal(TextureAddressMode.Wrap, sampler.AddressV);
        Assert.Equal(TextureAddressMode.Wrap, sampler.AddressW);
    }

    [Fact]
    public void Get_AnisotropicWrap_HasCorrectAnisotropy()
    {
        var manager = new SamplerManager(8);
        var sampler = manager.Get("AnisotropicWrap");

        Assert.Equal(TextureFilterMode.Anisotropic, sampler.Filter);
        Assert.Equal(8, sampler.MaxAnisotropy);
    }

    [Fact]
    public void Get_ThrowsOnUnknown()
    {
        var manager = new SamplerManager();
        Assert.Throws<KeyNotFoundException>(() => manager.Get("NonExistent"));
    }

    [Fact]
    public void TryGet_ReturnsTrue_WhenFound()
    {
        var manager = new SamplerManager();
        var found = manager.TryGet("LinearClamp", out var desc);

        Assert.True(found);
        Assert.Equal(TextureFilterMode.Linear, desc.Filter);
        Assert.Equal(TextureAddressMode.Clamp, desc.AddressU);
    }

    [Fact]
    public void TryGet_ReturnsFalse_WhenNotFound()
    {
        var manager = new SamplerManager();
        Assert.False(manager.TryGet("Nope", out _));
    }

    [Fact]
    public void Register_AddsNewSampler()
    {
        var manager = new SamplerManager();
        var custom = new SamplerDescription(TextureFilterMode.Point,
            TextureAddressMode.Mirror, TextureAddressMode.Mirror, TextureAddressMode.Mirror);

        manager.Register("PointMirror", custom);

        Assert.True(manager.HasSampler("PointMirror"));
        Assert.Equal(8, manager.SamplerCount);
        Assert.Equal(custom, manager.Get("PointMirror"));
    }

    [Fact]
    public void Register_OverwritesExisting()
    {
        var manager = new SamplerManager();
        var replacement = SamplerDescription.PointClamp;

        manager.Register("LinearWrap", replacement);

        var result = manager.Get("LinearWrap");
        Assert.Equal(TextureFilterMode.Point, result.Filter);
        Assert.Equal(TextureAddressMode.Clamp, result.AddressU);
    }

    [Fact]
    public void Register_ThrowsOnNullName()
    {
        var manager = new SamplerManager();
        Assert.Throws<ArgumentNullException>(() =>
            manager.Register(null!, SamplerDescription.LinearWrap));
    }

    [Fact]
    public void Register_ThrowsOnEmptyName()
    {
        var manager = new SamplerManager();
        Assert.Throws<ArgumentException>(() =>
            manager.Register("", SamplerDescription.LinearWrap));
    }

    [Fact]
    public void GetStaticSamplers_Returns6()
    {
        var manager = new SamplerManager();
        var statics = manager.GetStaticSamplers();
        Assert.Equal(6, statics.Count);
    }

    [Fact]
    public void GetStaticSamplers_ContainsAllModes()
    {
        var manager = new SamplerManager(4);
        var statics = manager.GetStaticSamplers();

        Assert.Contains(statics, s => s.Filter == TextureFilterMode.Linear && s.AddressU == TextureAddressMode.Wrap);
        Assert.Contains(statics, s => s.Filter == TextureFilterMode.Linear && s.AddressU == TextureAddressMode.Clamp);
        Assert.Contains(statics, s => s.Filter == TextureFilterMode.Point && s.AddressU == TextureAddressMode.Wrap);
        Assert.Contains(statics, s => s.Filter == TextureFilterMode.Point && s.AddressU == TextureAddressMode.Clamp);
        Assert.Contains(statics, s => s.Filter == TextureFilterMode.Anisotropic && s.AddressU == TextureAddressMode.Wrap && s.MaxAnisotropy == 4);
        Assert.Contains(statics, s => s.Filter == TextureFilterMode.Anisotropic && s.AddressU == TextureAddressMode.Clamp && s.MaxAnisotropy == 4);
    }

    [Fact]
    public void SamplerDescription_Presets_AreCorrect()
    {
        var lw = SamplerDescription.LinearWrap;
        Assert.Equal(TextureFilterMode.Linear, lw.Filter);
        Assert.Equal(TextureAddressMode.Wrap, lw.AddressU);

        var pc = SamplerDescription.PointClamp;
        Assert.Equal(TextureFilterMode.Point, pc.Filter);
        Assert.Equal(TextureAddressMode.Clamp, pc.AddressU);

        var aw = SamplerDescription.AnisotropicWrap(8);
        Assert.Equal(TextureFilterMode.Anisotropic, aw.Filter);
        Assert.Equal(8, aw.MaxAnisotropy);
    }

    [Fact]
    public void Samplers_ReadOnlyDictionary_Accessible()
    {
        var manager = new SamplerManager();
        var dict = manager.Samplers;

        Assert.NotNull(dict);
        Assert.True(dict.Count >= 7);
        Assert.True(dict.ContainsKey("LinearWrap"));
    }
}


