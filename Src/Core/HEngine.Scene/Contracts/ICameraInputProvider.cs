using System.Numerics;

namespace HEngine.Core.Contracts;

public interface ICameraInputProvider
{
    Vector3 GetMovementAxes();
    Vector2 GetLookDelta();
}