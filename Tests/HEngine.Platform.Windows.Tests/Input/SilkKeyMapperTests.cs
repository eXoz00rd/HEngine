using HEngine.Platform.Input;
using HEngine.Platform.Windows.Input;
using SilkKey = Silk.NET.Input.Key;

namespace HEngine.Platform.Windows.Tests.Input;

public class SilkKeyMapperTests
{
    [Theory]
    [InlineData(SilkKey.A, Key.A)]
    [InlineData(SilkKey.Z, Key.Z)]
    [InlineData(SilkKey.Number0, Key.Number0)]
    [InlineData(SilkKey.Number9, Key.Number9)]
    [InlineData(SilkKey.F1, Key.F1)]
    [InlineData(SilkKey.F12, Key.F12)]
    [InlineData(SilkKey.Space, Key.Space)]
    [InlineData(SilkKey.ControlLeft, Key.ControlLeft)]
    [InlineData(SilkKey.BackSlash, Key.Backslash)]
    [InlineData(SilkKey.GraveAccent, Key.GraveAccent)]
    public void Map_TranslatesKnownKeys(SilkKey silkKey, Key expected)
    {
        Assert.Equal(expected, SilkKeyMapper.Map(silkKey));
    }

    [Theory]
    [InlineData(SilkKey.Unknown)]
    [InlineData(SilkKey.CapsLock)]
    [InlineData(SilkKey.SuperLeft)]
    [InlineData(SilkKey.Keypad0)]
    [InlineData(SilkKey.Slash)]
    public void Map_UnrepresentedKeysMapToUnknown(SilkKey silkKey)
    {
        Assert.Equal(Key.Unknown, SilkKeyMapper.Map(silkKey));
    }
}
