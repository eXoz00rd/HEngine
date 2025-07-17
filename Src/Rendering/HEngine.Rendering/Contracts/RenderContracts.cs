using System.Numerics;

namespace HEngine.Rendering.Contracts;

public interface IRenderDevice : IDisposable {
    bool ShouldClose { get; }
    void Initialize(int width, int height, string title);
    void BeginFrame();
    void EndFrame();
    void Clear(Vector4 clearColor);
    void Present();
}

public interface IRenderCommandList : IDisposable {
    void SetViewMatrix(Matrix4x4 viewMatrix);
    void SetProjectionMatrix(Matrix4x4 projectionMatrix);
    void Reset();
    void Close();
}

public interface IRenderBatch<T> : IDisposable where T : struct {
    int Count { get; }
    void Add(T item);
    void Clear();
    void Render(IRenderCommandList commandList);
}

public interface IRenderResource : IDisposable {
    bool IsInitialized { get; }
    void Initialize(IRenderDevice device);
}