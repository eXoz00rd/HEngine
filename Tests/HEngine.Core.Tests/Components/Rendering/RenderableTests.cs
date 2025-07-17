using HEngine.Core.Components.Rendering;
using HEngine.Core.Contracts;

namespace HEngine.Core.Tests.Components.Rendering;

public class RenderableTests
{
    [Fact]
    public void DefaultConstructor_SetsDefaultValues()
    {
        var renderable = new Renderable();
    
        Assert.False(renderable.IsVisible);
        Assert.Equal(0, renderable.Layer);
        Assert.Equal(0u, renderable.MaterialId);
        Assert.Equal(0u, renderable.MeshId);
        Assert.False(renderable.CastShadows);
        Assert.False(renderable.ReceiveShadows);
        Assert.Equal(0f, renderable.LodBias);
        Assert.Equal(RenderingMode.Opaque, renderable.Mode);
    }

    [Fact]
    public void Constructor_WithParameters_SetsCorrectValues()
    {
        var renderable = new Renderable(isVisible: true, layer: 5);
    
        Assert.True(renderable.IsVisible);
        Assert.Equal(5, renderable.Layer);
        Assert.Equal(0u, renderable.MaterialId);
        Assert.Equal(0u, renderable.MeshId);
        Assert.True(renderable.CastShadows);
        Assert.True(renderable.ReceiveShadows);
        Assert.Equal(1f, renderable.LodBias);
        Assert.Equal(RenderingMode.Opaque, renderable.Mode);
    }

    [Fact]
    public void Constructor_WithCustomValues_SetsCorrectValues()
    {
        var renderable = new Renderable(isVisible: false, layer: 5);
        
        Assert.False(renderable.IsVisible);
        Assert.Equal(5, renderable.Layer);
        Assert.Equal(0u, renderable.MaterialId);
        Assert.Equal(0u, renderable.MeshId);
        Assert.True(renderable.CastShadows);
        Assert.True(renderable.ReceiveShadows);
        Assert.Equal(1f, renderable.LodBias);
        Assert.Equal(RenderingMode.Opaque, renderable.Mode);
    }

    [Fact]
    public void Properties_CanBeModified()
    {
        var renderable = new Renderable
        {
            IsVisible = false,
            Layer = 10,
            MaterialId = 123,
            MeshId = 456,
            CastShadows = false,
            ReceiveShadows = false,
            LodBias = 2.5f,
            Mode = RenderingMode.Transparent
        };
        
        Assert.False(renderable.IsVisible);
        Assert.Equal(10, renderable.Layer);
        Assert.Equal(123u, renderable.MaterialId);
        Assert.Equal(456u, renderable.MeshId);
        Assert.False(renderable.CastShadows);
        Assert.False(renderable.ReceiveShadows);
        Assert.Equal(2.5f, renderable.LodBias);
        Assert.Equal(RenderingMode.Transparent, renderable.Mode);
    }

    [Fact]
    public void RenderingMode_HasCorrectValues()
    {
        Assert.Equal(0, (int)RenderingMode.Opaque);
        Assert.Equal(1, (int)RenderingMode.Transparent);
        Assert.Equal(2, (int)RenderingMode.Cutout);
        Assert.Equal(3, (int)RenderingMode.Additive);
    }

    [Fact]
    public void Renderable_ImplementsIComponent()
    {
        var renderable = new Renderable();
        Assert.IsType<IComponent>(renderable, exactMatch: false);
    }
}
