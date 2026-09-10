using HEngine.Platform.Input;
using SilkMouseButton = Silk.NET.Input.MouseButton;

namespace HEngine.Platform.Windows.Input;

public static class SilkMouseButtonMapper
{
    public static bool TryMap(SilkMouseButton button, out MouseButton mapped)
    {
        switch (button)
        {
            case SilkMouseButton.Left:
                mapped = MouseButton.Left;
                return true;
            case SilkMouseButton.Right:
                mapped = MouseButton.Right;
                return true;
            case SilkMouseButton.Middle:
                mapped = MouseButton.Middle;
                return true;
            case SilkMouseButton.Button4:
                mapped = MouseButton.Button4;
                return true;
            case SilkMouseButton.Button5:
                mapped = MouseButton.Button5;
                return true;
            default:
                mapped = default;
                return false;
        }
    }
}
