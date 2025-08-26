using System.Numerics;

namespace HEngine.Core.Rendering.Contracts;

public interface IRenderDevice : IDisposable
{
    bool IsInitialized { get; }
    bool ShouldClose { get; }

    void Initialize(int width, int height, string title);
    void BeginFrame();
    void EndFrame();
    void Present();
    void Clear(Vector4 clearColor);

    ICommandQueue GetCommandQueue();
}