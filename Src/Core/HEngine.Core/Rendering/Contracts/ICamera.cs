using System.Numerics;

namespace HEngine.Core.Rendering.Contracts;

/// <summary>
/// Simple camera abstraction providing view and projection matrices.
/// Rendering systems and pipeline should use these matrices via the active camera.
/// </summary>
public interface ICamera
{
    Matrix4x4 ViewMatrix { get; }
    Matrix4x4 ProjectionMatrix { get; }
}