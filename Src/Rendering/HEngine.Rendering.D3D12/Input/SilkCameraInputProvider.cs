using System.Numerics;
using HEngine.Core.Contracts;

namespace HEngine.Rendering.Input;

public sealed class SilkCameraInputProvider : ICameraInputProvider
{
    private readonly InputState _state;

    public SilkCameraInputProvider(InputState state)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
    }

    public Vector3 GetMovementAxes() => _state.GetMovementAxes();
    public Vector2 GetLookDelta() => _state.ConsumeLookDelta();
}
