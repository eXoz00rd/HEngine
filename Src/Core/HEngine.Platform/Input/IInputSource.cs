using System.Numerics;

namespace HEngine.Platform.Input;

public interface IInputSource
{
    event Action<Key>? KeyDown;
    event Action<Key>? KeyUp;
    event Action<MouseButton>? MouseButtonDown;
    event Action<MouseButton>? MouseButtonUp;
    event Action<Vector2>? MouseMoved;
}
