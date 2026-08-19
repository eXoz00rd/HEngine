using HEngine.Rendering.Data;
using HEngine.Rendering.Managers;

namespace HEngine.Rendering.Tests.Managers;

public class MaterialManagerTests
{
    [Fact]
    public void TryGetById_WithIdZero_AlwaysFails()
    {
        var manager = new MaterialManager();
        manager.RegisterWithId("test", new Material());

        var found = manager.TryGetById(0, out var name, out var material);

        Assert.False(found);
        Assert.Null(name);
        Assert.Null(material);
    }

    [Fact]
    public void RegisterWithId_SameNameTwice_ReturnsSameId()
    {
        var manager = new MaterialManager();

        var firstId = manager.RegisterWithId("checker", new Material());
        var secondId = manager.RegisterWithId("checker", new Material());

        Assert.Equal(firstId, secondId);
        Assert.NotEqual(0u, firstId);
    }

    [Fact]
    public void RegisterWithId_DifferentNames_ReturnDifferentIds()
    {
        var manager = new MaterialManager();

        var id1 = manager.RegisterWithId("a", new Material());
        var id2 = manager.RegisterWithId("b", new Material());

        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void TryGetById_RoundTrips_ToRegisteredMaterial()
    {
        var manager = new MaterialManager();
        var material = new Material { DiffuseColor = new System.Numerics.Vector4(0.2f, 0.4f, 0.6f, 1f) };

        var id = manager.RegisterWithId("checker", material);
        var found = manager.TryGetById(id, out var name, out var resolved);

        Assert.True(found);
        Assert.Equal("checker", name);
        Assert.Same(material, resolved);
    }

    [Fact]
    public void TryGetById_WithUnknownId_ReturnsFalse()
    {
        var manager = new MaterialManager();

        var found = manager.TryGetById(999, out var name, out var material);

        Assert.False(found);
        Assert.Null(name);
        Assert.Null(material);
    }

    [Fact]
    public void Remove_AlsoRemovesIdRegistration()
    {
        var manager = new MaterialManager();
        var id = manager.RegisterWithId("removable", new Material());

        manager.Remove("removable");

        Assert.False(manager.TryGetById(id, out _, out _));
    }
}
