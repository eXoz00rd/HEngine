using System.Numerics;

namespace HEngine.Core.Rendering.Contracts;

public interface IRenderManager : IDisposable
{
    bool ShouldClose { get; }
    bool CanRender { get; }
    bool IsInitialized { get; }
    
    void Initialize(int width, int height, string title);
    void UpdateInput();
    void BeginRender();
    void EndRender();
    void Clear(Vector4 clearColor);
    void Present();
    IRenderContext GetRenderContext();
}