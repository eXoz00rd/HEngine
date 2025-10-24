using System.Collections.Generic;
using System.Numerics;
using Silk.NET.Input;

namespace HEngine.Rendering.Input;

public sealed class InputState
{
    private readonly object _lock = new();
    private readonly HashSet<Key> _pressed = new();
    private Vector2 _lastMousePos;
    private bool _hasLastMouse;
    private Vector2 _lookDeltaAccum;

    public void OnKeyDown(Key key)
    {
        lock (_lock)
        {
            _pressed.Add(key);
        }
    }

    public void OnKeyUp(Key key)
    {
        lock (_lock)
        {
            _pressed.Remove(key);
        }
    }

    public void OnMouseMove(IMouse mouse, Vector2 position)
    {
        lock (_lock)
        {
            if (_hasLastMouse)
                _lookDeltaAccum += position - _lastMousePos;
            _lastMousePos = position;
            _hasLastMouse = true;
        }
    }

    public Vector3 GetMovementAxes()
    {
        lock (_lock)
        {
            float x = 0, y = 0, z = 0;
            if (_pressed.Contains(Key.A)) x -= 1f;
            if (_pressed.Contains(Key.D)) x += 1f;

            if (_pressed.Contains(Key.Q) || _pressed.Contains(Key.ControlLeft) || _pressed.Contains(Key.ControlRight)) y -= 1f;
            if (_pressed.Contains(Key.E) || _pressed.Contains(Key.Space)) y += 1f;

            if (_pressed.Contains(Key.S)) z -= 1f;
            if (_pressed.Contains(Key.W)) z += 1f;

            var v = new Vector3(x, y, z);
            var lenSq = v.LengthSquared();
            if (lenSq > 1f)
                v = Vector3.Normalize(v);
            return v;
        }
    }

    public Vector2 ConsumeLookDelta()
    {
        lock (_lock)
        {
            var d = _lookDeltaAccum;
            _lookDeltaAccum = Vector2.Zero;
            return d;
        }
    }
}
