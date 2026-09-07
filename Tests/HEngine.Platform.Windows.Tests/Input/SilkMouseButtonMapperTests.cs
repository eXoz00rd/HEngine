using HEngine.Platform.Input;
using HEngine.Platform.Windows.Input;
using SilkMouseButton = Silk.NET.Input.MouseButton;

namespace HEngine.Platform.Windows.Tests.Input;

public class SilkMouseButtonMapperTests
{
    [Theory]
    [InlineData(SilkMouseButton.Left, MouseButton.Left)]
    [InlineData(SilkMouseButton.Right, MouseButton.Right)]
    [InlineData(SilkMouseButton.Middle, MouseButton.Middle)]
    [InlineData(SilkMouseButton.Button4, MouseButton.Button4)]
    [InlineData(SilkMouseButton.Button5, MouseButton.Button5)]
    public void TryMap_TranslatesKnownButtons(SilkMouseButton silkButton, MouseButton expected)
    {
        var result = SilkMouseButtonMapper.TryMap(silkButton, out var mapped);

        Assert.True(result);
        Assert.Equal(expected, mapped);
    }

    [Theory]
    [InlineData(SilkMouseButton.Button6)]
    [InlineData(SilkMouseButton.Unknown)]
    public void TryMap_UnrepresentedButtonsReturnFalse(SilkMouseButton silkButton)
    {
        var result = SilkMouseButtonMapper.TryMap(silkButton, out _);

        Assert.False(result);
    }
}
