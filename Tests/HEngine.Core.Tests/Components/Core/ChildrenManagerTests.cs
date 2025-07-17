using HEngine.Core.Components.Core;
using HEngine.Core.Primitives;

namespace HEngine.Core.Tests.Components.Core;

public class ChildrenTests {
    [Fact]
    public void Constructor_ShouldInitializeEmpty()
    {
        var children = new Children();

        Assert.Equal(0, children.Count);
    }

    [Fact]
    public void Add_SingleChild_ShouldAddToFirstSlot()
    {
        var children = new Children();
        var child = new Entity(1);


        children.Add(child);


        Assert.Equal(1, children.Count);
        Assert.Equal(child, children.GetChild(0));
    }

    [Fact]
    public void Add_NullEntity_ShouldNotAdd()
    {
        var children = new Children();


        children.Add(Entity.Null);


        Assert.Equal(0, children.Count);
    }

    [Fact]
    public void Add_FourChildren_ShouldFillAllSlots()
    {
        var children = new Children();
        var child1 = new Entity(1);
        var child2 = new Entity(2);
        var child3 = new Entity(3);
        var child4 = new Entity(4);


        children.Add(child1);
        children.Add(child2);
        children.Add(child3);
        children.Add(child4);


        Assert.Equal(4, children.Count);
        Assert.Equal(child1, children.GetChild(0));
        Assert.Equal(child2, children.GetChild(1));
        Assert.Equal(child3, children.GetChild(2));
        Assert.Equal(child4, children.GetChild(3));
    }

    [Fact]
    public void Add_MoreThanFourChildren_ShouldUseAdditionalList()
    {
        var children = new Children();
        var entities = new Entity[6];
        for (var i = 0; i < 6; i++)
            entities[i] = new Entity((uint)(i + 1));


        foreach (var entity in entities)
            children.Add(entity);


        Assert.Equal(6, children.Count);
        for (var i = 0; i < 6; i++)
            Assert.Equal(entities[i], children.GetChild(i));
    }

    [Fact]
    public void Remove_ExistingChild_ShouldRemoveAndReturnTrue()
    {
        var children = new Children();
        var child1 = new Entity(1);
        var child2 = new Entity(2);
        children.Add(child1);
        children.Add(child2);


        var result = children.Remove(child1);


        Assert.True(result);
        Assert.Equal(1, children.Count);
        Assert.Equal(child2, children.GetChild(0));
    }

    [Fact]
    public void Remove_NonExistingChild_ShouldReturnFalse()
    {
        var children = new Children();
        var child1 = new Entity(1);
        var nonExisting = new Entity(999);
        children.Add(child1);


        var result = children.Remove(nonExisting);


        Assert.False(result);
        Assert.Equal(1, children.Count);
    }

    [Fact]
    public void Remove_NullEntity_ShouldReturnFalse()
    {
        var children = new Children();


        var result = children.Remove(Entity.Null);


        Assert.False(result);
    }

    [Fact]
    public void Remove_FromAdditionalList_ShouldRemoveCorrectly()
    {
        var children = new Children();
        var entities = new Entity[6];
        for (var i = 0; i < 6; i++)
        {
            entities[i] = new Entity((uint)(i + 1));
            children.Add(entities[i]);
        }


        var result = children.Remove(entities[4]); // Remove 5th child (in additional list)


        Assert.True(result);
        Assert.Equal(5, children.Count);
        Assert.NotEqual(entities[4], children.GetChild(4));
    }

    [Fact]
    public void Clear_ShouldRemoveAllChildren()
    {
        var children = new Children();
        children.Add(new Entity(1));
        children.Add(new Entity(2));
        children.Add(new Entity(3));


        children.Clear();


        Assert.Equal(0, children.Count);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5)]
    public void GetChild_OutOfRange_ShouldReturnNull(int index)
    {
        var children = new Children();
        children.Add(new Entity(1));


        var result = children.GetChild(index);


        Assert.Equal(Entity.Null, result);
    }

    [Fact]
    public void GetChild_ValidIndex_ShouldReturnCorrectChild()
    {
        var children = new Children();
        var expectedChild = new Entity(42);
        children.Add(new Entity(1));
        children.Add(expectedChild);


        var result = children.GetChild(1);


        Assert.Equal(expectedChild, result);
    }

    [Fact]
    public void Remove_FirstChild_ShouldShiftOthersLeft()
    {
        var children = new Children();
        var child1 = new Entity(1);
        var child2 = new Entity(2);
        var child3 = new Entity(3);
        children.Add(child1);
        children.Add(child2);
        children.Add(child3);


        children.Remove(child1);


        Assert.Equal(2, children.Count);
        Assert.Equal(child2, children.GetChild(0));
        Assert.Equal(child3, children.GetChild(1));
    }
}