using System.Numerics;

namespace HEngine.Core.Rendering.Contracts;

public interface IRenderCommandList : IDisposable {
    void SetViewMatrix(Matrix4x4 viewMatrix);
    void SetProjectionMatrix(Matrix4x4 projectionMatrix);
    void Reset();
    void Close();
}