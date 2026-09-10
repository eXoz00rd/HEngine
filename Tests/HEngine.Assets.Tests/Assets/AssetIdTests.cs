using HEngine.Assets.Assets;

namespace HEngine.Core.Tests.Assets;

public class AssetIdTests
{
    [Fact]
    public void New_ReturnsDistinctIds()
    {
        var first = AssetId.New();
        var second = AssetId.New();

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Equals_SameUnderlyingGuid_AreEqual()
    {
        var guid = Guid.NewGuid();
        var first = new AssetId(guid);
        var second = new AssetId(guid);

        Assert.Equal(first, second);
        Assert.True(first == second);
        Assert.False(first != second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void ToString_MatchesUnderlyingGuid()
    {
        var guid = Guid.NewGuid();
        var id = new AssetId(guid);

        Assert.Equal(guid.ToString(), id.ToString());
    }
}
