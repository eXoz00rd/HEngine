using System.Numerics;

namespace HEngine.Core.Rendering.Contracts;

public interface ICamera
{
    Matrix4x4 ViewMatrix { get; }
    Matrix4x4 ProjectionMatrix { get; }
}