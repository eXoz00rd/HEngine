using HEngine.Core.Components.Core;
using Xunit;

namespace HEngine.Core.Tests.Components.Core;

public class DirtyFlagTests
{
    private const uint Flag1 = 0x01;
    private const uint Flag2 = 0x02;
    private const uint Flag4 = 0x04;
    private const uint Flag8 = 0x08;
    
    [Fact]
    public void Constructor_Default_ShouldHaveNoFlags()
    {
        
        var dirtyFlag = new DirtyFlag();
        
        
        Assert.Equal(0u, dirtyFlag.Flags);
        Assert.False(dirtyFlag.HasAnyFlags);
        Assert.True(dirtyFlag.IsValid);
    }
    
    [Theory]
    [InlineData(0u)]
    [InlineData(Flag1)]
    [InlineData(Flag1 | Flag2)]
    public void Constructor_WithFlags_ShouldSetCorrectValue(uint flags)
    {
        
        var dirtyFlag = new DirtyFlag(flags);
        
        
        Assert.Equal(flags, dirtyFlag.Flags);
        Assert.Equal(flags != 0, dirtyFlag.HasAnyFlags);
    }
    
    [Theory]
    [InlineData(Flag1, true)]
    [InlineData(Flag2, false)]
    public void HasFlag_ShouldReturnCorrectValue(uint testFlag, bool expected)
    {
        
        var dirtyFlag = new DirtyFlag(Flag1);
        
        
        var result = dirtyFlag.HasFlag(testFlag);
        
        
        Assert.Equal(expected, result);
    }
    
    [Fact]
    public void SetFlag_ShouldAddFlag()
    {
        
        var dirtyFlag = new DirtyFlag();
        
        
        dirtyFlag.SetFlag(Flag1);
        
        
        Assert.True(dirtyFlag.HasFlag(Flag1));
        Assert.Equal(Flag1, dirtyFlag.Flags);
    }
    
    [Fact]
    public void SetFlag_Multiple_ShouldAddAllFlags()
    {
        
        var dirtyFlag = new DirtyFlag();
        
        
        dirtyFlag.SetFlag(Flag1);
        dirtyFlag.SetFlag(Flag2);
        
        
        Assert.True(dirtyFlag.HasFlag(Flag1));
        Assert.True(dirtyFlag.HasFlag(Flag2));
        Assert.Equal(Flag1 | Flag2, dirtyFlag.Flags);
    }
    
    [Fact]
    public void ClearFlag_ShouldRemoveFlag()
    {
        
        var dirtyFlag = new DirtyFlag(Flag1 | Flag2);
        
        
        dirtyFlag.ClearFlag(Flag1);
        
        
        Assert.False(dirtyFlag.HasFlag(Flag1));
        Assert.True(dirtyFlag.HasFlag(Flag2));
        Assert.Equal(Flag2, dirtyFlag.Flags);
    }
    
    [Fact]
    public void Clear_ShouldRemoveAllFlags()
    {
        
        var dirtyFlag = new DirtyFlag(Flag1 | Flag2 | Flag4);
        
        
        dirtyFlag.Clear();
        
        
        Assert.Equal(0u, dirtyFlag.Flags);
        Assert.False(dirtyFlag.HasAnyFlags);
    }
    
    [Theory]
    [InlineData(Flag1 | Flag2, Flag1 | Flag2, true)]
    [InlineData(Flag1 | Flag2, Flag1, true)]
    [InlineData(Flag1, Flag1 | Flag2, false)]
    [InlineData(0u, Flag1, false)]
    public void HasAllFlags_ShouldReturnCorrectValue(uint currentFlags, uint testFlags, bool expected)
    {
        
        var dirtyFlag = new DirtyFlag(currentFlags);
        
        
        var result = dirtyFlag.HasAllFlags(testFlags);
        
        
        Assert.Equal(expected, result);
    }
    
    [Theory]
    [InlineData(Flag1 | Flag2, Flag1 | Flag4, true)]
    [InlineData(Flag1 | Flag2, Flag4 | Flag8, false)]
    [InlineData(0u, Flag1, false)]
    public void HasAnyFlag_ShouldReturnCorrectValue(uint currentFlags, uint testFlags, bool expected)
    {
        
        var dirtyFlag = new DirtyFlag(currentFlags);
        
        
        var result = dirtyFlag.HasAnyFlag(testFlags);
        
        
        Assert.Equal(expected, result);
    }
    
    [Fact]
    public void IsValid_ShouldAlwaysBeTrue()
    {
        
        var dirtyFlag1 = new DirtyFlag();
        var dirtyFlag2 = new DirtyFlag(Flag1 | Flag2);
        
        
        Assert.True(dirtyFlag1.IsValid);
        Assert.True(dirtyFlag2.IsValid);
    }
}
