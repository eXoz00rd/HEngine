using System.Numerics;
using HEngine.Platform.Input;
using HEngine.Platform.Windowing;
using HEngine.Platform.Windows.Windowing;
using SilkIInputContext = Silk.NET.Input.IInputContext;
using SilkIKeyboard = Silk.NET.Input.IKeyboard;
using SilkIMouse = Silk.NET.Input.IMouse;
using SilkKey = Silk.NET.Input.Key;
using SilkMouseButton = Silk.NET.Input.MouseButton;

namespace HEngine.Platform.Windows.Input;

public sealed class SilkInputSource : IInputSource, IDisposable
{
    private readonly SilkIInputContext _inputContext;
    private bool _disposed;

    public SilkInputSource(IWindow window)
    {
        ArgumentNullException.ThrowIfNull(window);

        if (window is not SilkWindow silkWindow)
        {
            throw new ArgumentException(
                $"{nameof(SilkInputSource)} requires a {nameof(SilkWindow)}, got {window.GetType().Name}.",
                nameof(window));
        }

        _inputContext = Silk.NET.Input.InputWindowExtensions.CreateInput(silkWindow.NativeWindow);

        foreach (var keyboard in _inputContext.Keyboards)
        {
            keyboard.KeyDown += OnKeyDown;
            keyboard.KeyUp += OnKeyUp;
        }

        foreach (var mouse in _inputContext.Mice)
        {
            mouse.MouseDown += OnMouseDown;
            mouse.MouseUp += OnMouseUp;
            mouse.MouseMove += OnMouseMove;
        }
    }

    public event Action<Key>? KeyDown;
    public event Action<Key>? KeyUp;
    public event Action<MouseButton>? MouseButtonDown;
    public event Action<MouseButton>? MouseButtonUp;
    public event Action<Vector2>? MouseMoved;

    private void OnKeyDown(SilkIKeyboard keyboard, SilkKey key, int scancode) =>
        KeyDown?.Invoke(SilkKeyMapper.Map(key));

    private void OnKeyUp(SilkIKeyboard keyboard, SilkKey key, int scancode) =>
        KeyUp?.Invoke(SilkKeyMapper.Map(key));

    private void OnMouseDown(SilkIMouse mouse, SilkMouseButton button)
    {
        if (SilkMouseButtonMapper.TryMap(button, out var mapped))
        {
            MouseButtonDown?.Invoke(mapped);
        }
    }

    private void OnMouseUp(SilkIMouse mouse, SilkMouseButton button)
    {
        if (SilkMouseButtonMapper.TryMap(button, out var mapped))
        {
            MouseButtonUp?.Invoke(mapped);
        }
    }

    private void OnMouseMove(SilkIMouse mouse, Vector2 position) =>
        MouseMoved?.Invoke(position);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var keyboard in _inputContext.Keyboards)
        {
            keyboard.KeyDown -= OnKeyDown;
            keyboard.KeyUp -= OnKeyUp;
        }

        foreach (var mouse in _inputContext.Mice)
        {
            mouse.MouseDown -= OnMouseDown;
            mouse.MouseUp -= OnMouseUp;
            mouse.MouseMove -= OnMouseMove;
        }

        _inputContext.Dispose();
        _disposed = true;
    }
}
