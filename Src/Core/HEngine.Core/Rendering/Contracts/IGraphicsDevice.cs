using System.Numerics;

namespace HEngine.Core.Rendering.Contracts;

public interface IGraphicsDevice : IDisposable
{
    bool IsInitialized { get; }
    bool ShouldClose { get; }
    void Initialize(int width, int height, string title);
    void BeginFrame();
    void EndFrame();
    void Clear(Vector4 clearColor);
    void Present();
    ICommandQueue GetCommandQueue();
}