using HEngine.Core.Components.Core;
using HEngine.Core.Primitives;

namespace HEngine.Core.Tests.Components.Core;

public class ParentTests {
    [Fact]
    public void Constructor_WithValidEntity_ShouldSetValue()
    {
        var expectedEntity = new Entity(42);

        var parent = new Parent(expectedEntity);

        Assert.Equal(expectedEntity, parent.Value);
        Assert.True(parent.IsValid);
    }

    [Fact]
    public void Constructor_WithNullEntity_ShouldSetValue()
    {
        var nullEntity = Entity.Null;

        var parent = new Parent(nullEntity);

        Assert.Equal(nullEntity, parent.Value);
        Assert.False(parent.IsValid);
    }

    [Fact]
    public void ImplicitConversion_ToEntity_ShouldReturnValue()
    {
        var expectedEntity = new Entity(42);
        var parent = new Parent(expectedEntity);

        Entity result = parent;

        Assert.Equal(expectedEntity, result);
    }

    [Fact]
    public void ImplicitConversion_FromEntity_ShouldCreateCorrectParent()
    {
        var expectedEntity = new Entity(42);

        Parent parent = expectedEntity;

        Assert.Equal(expectedEntity, parent.Value);
    }

    [Theory]
    [InlineData(42, true)]
    [InlineData(1, true)]
    public void IsValid_ShouldReturnCorrectValue(uint entityId, bool expectedValid)
    {
        
        var entity = new Entity(entityId);
        var parent = new Parent(entity);

         
        Assert.Equal(expectedValid, parent.IsValid);
    }

    [Fact]
    public void IsValid_WithNullEntity_ShouldReturnFalse()
    {
        
        var parent = new Parent(Entity.Null);

         
        Assert.False(parent.IsValid);
    }

    [Fact]
    public void IsValid_WithZeroIdButDifferentGeneration_ShouldReturnTrue()
    {
        
        var entity = new Entity(0);
        var parent = new Parent(entity);

         
        Assert.True(
            parent.IsValid
        );
    }
}