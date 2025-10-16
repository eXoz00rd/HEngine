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

    // Returns the current render context; may throw or be null depending on initialization state.
    IRenderContext GetRenderContext();

    // Try-pattern to avoid double null checks; returns true when a valid context is available.
    bool TryGetRenderContext(out IRenderContext context);

    // Camera management: allows pipeline to pull matrices from the active camera when available.
    void SetActiveCamera(ICamera camera);
    bool TryGetActiveCamera(out ICamera camera);
}