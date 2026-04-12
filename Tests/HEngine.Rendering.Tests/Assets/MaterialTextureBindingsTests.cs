using HEngine.Rendering.Data;
using HEngine.Rendering.Enums;

namespace HEngine.Rendering.Tests.Assets;

public class MaterialTextureBindingsTests
{
    [Fact]
    public void New_HasNoBindings()
    {
        var bindings = new MaterialTextureBindings();
        Assert.Equal(0, bindings.BoundCount);
    }

    [Fact]
    public void Bind_AddsBinding()
    {
        var bindings = new MaterialTextureBindings();
        bindings.Bind(TextureSlot.DiffuseMap, 42, "test.png");

        Assert.Equal(1, bindings.BoundCount);
        Assert.True(bindings.HasBinding(TextureSlot.DiffuseMap));
    }

    [Fact]
    public void TryGetBinding_ReturnsCorrectData()
    {
        var bindings = new MaterialTextureBindings();
        bindings.Bind(TextureSlot.NormalMap, 7, "normal.png");

        Assert.True(bindings.TryGetBinding(TextureSlot.NormalMap, out var binding));
        Assert.Equal(7, binding.TextureHandle);
        Assert.Equal("normal.png", binding.SourcePath);
    }

    [Fact]
    public void TryGetBinding_ReturnsFalse_WhenNotBound()
    {
        var bindings = new MaterialTextureBindings();
        Assert.False(bindings.TryGetBinding(TextureSlot.DiffuseMap, out _));
    }

    [Fact]
    public void HasBinding_ReturnsFalse_WhenNotBound()
    {
        var bindings = new MaterialTextureBindings();
        Assert.False(bindings.HasBinding(TextureSlot.EmissiveMap));
    }

    [Fact]
    public void Bind_OverwritesExisting()
    {
        var bindings = new MaterialTextureBindings();
        bindings.Bind(TextureSlot.DiffuseMap, 1, "old.png");
        bindings.Bind(TextureSlot.DiffuseMap, 2, "new.png");

        Assert.Equal(1, bindings.BoundCount);
        Assert.True(bindings.TryGetBinding(TextureSlot.DiffuseMap, out var binding));
        Assert.Equal(2, binding.TextureHandle);
        Assert.Equal("new.png", binding.SourcePath);
    }

    [Fact]
    public void Unbind_RemovesBinding()
    {
        var bindings = new MaterialTextureBindings();
        bindings.Bind(TextureSlot.DiffuseMap, 1);
        bindings.Unbind(TextureSlot.DiffuseMap);

        Assert.Equal(0, bindings.BoundCount);
        Assert.False(bindings.HasBinding(TextureSlot.DiffuseMap));
    }

    [Fact]
    public void Unbind_NonExistent_DoesNotThrow()
    {
        var bindings = new MaterialTextureBindings();
        var ex = Record.Exception(() => bindings.Unbind(TextureSlot.AOMap));
        Assert.Null(ex);
    }

    [Fact]
    public void Clear_RemovesAllBindings()
    {
        var bindings = new MaterialTextureBindings();
        bindings.Bind(TextureSlot.DiffuseMap, 1);
        bindings.Bind(TextureSlot.NormalMap, 2);
        bindings.Bind(TextureSlot.EmissiveMap, 3);

        bindings.Clear();

        Assert.Equal(0, bindings.BoundCount);
    }

    [Fact]
    public void GetAll_ReturnsAllBindings()
    {
        var bindings = new MaterialTextureBindings();
        bindings.Bind(TextureSlot.DiffuseMap, 1);
        bindings.Bind(TextureSlot.NormalMap, 2);

        var all = bindings.GetAll();
        Assert.Equal(2, all.Count);
        Assert.True(all.ContainsKey(TextureSlot.DiffuseMap));
        Assert.True(all.ContainsKey(TextureSlot.NormalMap));
    }

    [Fact]
    public void MultipleSlots_IndependentBindings()
    {
        var bindings = new MaterialTextureBindings();
        bindings.Bind(TextureSlot.DiffuseMap, 10, "diffuse.png");
        bindings.Bind(TextureSlot.NormalMap, 20, "normal.png");
        bindings.Bind(TextureSlot.MetallicRoughnessMap, 30, "mr.png");
        bindings.Bind(TextureSlot.EmissiveMap, 40, "emissive.png");
        bindings.Bind(TextureSlot.AOMap, 50, "ao.png");

        Assert.Equal(5, bindings.BoundCount);

        bindings.TryGetBinding(TextureSlot.MetallicRoughnessMap, out var mr);
        Assert.Equal(30, mr.TextureHandle);
        Assert.Equal("mr.png", mr.SourcePath);
    }

    // ─── ResolveFromPropertyBlock ────────────────────────────────────

    [Fact]
    public void ResolveFromPropertyBlock_MapsStandardProperties()
    {
        var block = new MaterialPropertyBlock();
        block.SetTexture("_DiffuseTexture", "albedo.png");
        block.SetTexture("_NormalTexture", "normal.png");

        var bindings = new MaterialTextureBindings();
        int resolveCount = 0;
        bindings.ResolveFromPropertyBlock(block, path =>
        {
            resolveCount++;
            return path == "albedo.png" ? 100 : 200;
        });

        Assert.Equal(2, resolveCount);
        Assert.Equal(2, bindings.BoundCount);

        Assert.True(bindings.TryGetBinding(TextureSlot.DiffuseMap, out var diffuse));
        Assert.Equal(100, diffuse.TextureHandle);

        Assert.True(bindings.TryGetBinding(TextureSlot.NormalMap, out var normal));
        Assert.Equal(200, normal.TextureHandle);
    }

    [Fact]
    public void ResolveFromPropertyBlock_IgnoresEmptyPaths()
    {
        var block = new MaterialPropertyBlock();
        // Don't set any textures

        var bindings = new MaterialTextureBindings();
        bindings.ResolveFromPropertyBlock(block, _ => 999);

        Assert.Equal(0, bindings.BoundCount);
    }

    [Fact]
    public void ResolveFromPropertyBlock_AllSlots()
    {
        var block = new MaterialPropertyBlock();
        block.SetTexture("_DiffuseTexture", "d.png");
        block.SetTexture("_NormalTexture", "n.png");
        block.SetTexture("_MetallicRoughnessTexture", "mr.png");
        block.SetTexture("_EmissiveTexture", "e.png");
        block.SetTexture("_AOTexture", "ao.png");

        var bindings = new MaterialTextureBindings();
        int counter = 0;
        bindings.ResolveFromPropertyBlock(block, _ => ++counter);

        Assert.Equal(5, bindings.BoundCount);
        Assert.True(bindings.HasBinding(TextureSlot.DiffuseMap));
        Assert.True(bindings.HasBinding(TextureSlot.NormalMap));
        Assert.True(bindings.HasBinding(TextureSlot.MetallicRoughnessMap));
        Assert.True(bindings.HasBinding(TextureSlot.EmissiveMap));
        Assert.True(bindings.HasBinding(TextureSlot.AOMap));
    }

    [Fact]
    public void ResolveFromPropertyBlock_ThrowsOnNullBlock()
    {
        var bindings = new MaterialTextureBindings();
        Assert.Throws<ArgumentNullException>(() =>
            bindings.ResolveFromPropertyBlock(null!, _ => 0));
    }

    [Fact]
    public void ResolveFromPropertyBlock_ThrowsOnNullResolver()
    {
        var bindings = new MaterialTextureBindings();
        Assert.Throws<ArgumentNullException>(() =>
            bindings.ResolveFromPropertyBlock(new MaterialPropertyBlock(), null!));
    }

    // ─── TextureSlot enum values ─────────────────────────────────────

    [Theory]
    [InlineData(TextureSlot.DiffuseMap, 0)]
    [InlineData(TextureSlot.NormalMap, 1)]
    [InlineData(TextureSlot.MetallicRoughnessMap, 2)]
    [InlineData(TextureSlot.EmissiveMap, 3)]
    [InlineData(TextureSlot.AOMap, 4)]
    [InlineData(TextureSlot.ShadowMap, 5)]
    [InlineData(TextureSlot.Custom0, 6)]
    [InlineData(TextureSlot.Custom1, 7)]
    public void TextureSlot_HasCorrectRegisterIndex(TextureSlot slot, int expected)
    {
        Assert.Equal(expected, (int)slot);
    }
}

