using System.Numerics;
using System.Runtime.InteropServices;
using HEngine.Rendering.Data;
using HEngine.Rendering.Managers;

namespace HEngine.Rendering.Tests.Managers;

public class ShadowManagerTests
{
    [Fact(DisplayName = "ShadowCbuffer size is multiple of 16 bytes")]
    public void ShadowCbuffer_Size_Is_Aligned()
    {
        var size = Marshal.SizeOf<ShadowCbuffer>();
        Assert.Equal(0, size % 16);
    }

    [Fact(DisplayName = "ShadowCbuffer Create sets cascade count correctly")]
    public void ShadowCbuffer_Create_Sets_CascadeCount()
    {
        Matrix4x4[] vps = [Matrix4x4.Identity, Matrix4x4.Identity, Matrix4x4.Identity, Matrix4x4.Identity];
        float[] splits = [10f, 50f, 100f, 200f];

        var cb = ShadowCbuffer.Create(vps, splits);

        Assert.Equal(4, cb.CascadeCount);
    }

    [Fact(DisplayName = "ShadowCbuffer Create stores cascade splits in Vector4")]
    public void ShadowCbuffer_Create_Stores_Splits()
    {
        Matrix4x4[] vps = [Matrix4x4.Identity, Matrix4x4.Identity];
        float[] splits = [25f, 100f];

        var cb = ShadowCbuffer.Create(vps, splits);

        Assert.Equal(25f, cb.CascadeSplits.X, 3);
        Assert.Equal(100f, cb.CascadeSplits.Y, 3);
    }

    [Fact(DisplayName = "ShadowCbuffer Create copies LightVP matrices")]
    public void ShadowCbuffer_Create_Copies_LightVP_Matrices()
    {
        var m = Matrix4x4.CreateTranslation(1f, 2f, 3f);
        Matrix4x4[] vps = [m, Matrix4x4.Identity, Matrix4x4.Identity, Matrix4x4.Identity];
        float[] splits = [10f, 50f, 100f, 200f];

        var cb = ShadowCbuffer.Create(vps, splits);

        Assert.Equal(m.M41, cb.LightVP0.M41, 5);
        Assert.Equal(m.M42, cb.LightVP0.M42, 5);
        Assert.Equal(m.M43, cb.LightVP0.M43, 5);
    }

    [Fact(DisplayName = "ShadowMapManager default resolution and cascade count")]
    public void ShadowMapManager_DefaultValues()
    {
        var manager = new ShadowMapManager();
        Assert.False(manager.IsInitialized);
        Assert.Equal(0, manager.Resolution);
        Assert.Equal(0, manager.CascadeCount);
    }

    [Fact(DisplayName = "SamplerManager registers ShadowComparison sampler by default")]
    public void SamplerManager_RegistersShadowComparisonSampler()
    {
        var manager = new SamplerManager();
        Assert.True(manager.HasSampler("ShadowComparison"));
        var desc = manager.Get("ShadowComparison");
        Assert.Equal(TextureFilterMode.Comparison, desc.Filter);
        Assert.Equal(TextureAddressMode.Clamp, desc.AddressU);
    }

    [Fact(DisplayName = "ShadowSettings defaults are sensible")]
    public void ShadowSettings_DefaultValues()
    {
        var settings = new HEngine.Core.Configuration.ShadowSettings();
        Assert.False(settings.Enabled);
        Assert.Equal(2048, settings.Resolution);
        Assert.Equal(4, settings.CascadeCount);
        Assert.True(settings.LambdaSplit > 0f && settings.LambdaSplit < 1f);
    }
}

