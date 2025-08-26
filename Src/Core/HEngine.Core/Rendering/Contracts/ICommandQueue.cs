using System.Numerics;
using HEngine.Core.Rendering.DirectX12.Contracts;

namespace HEngine.Core.Rendering.Contracts;

public interface ICommandQueue : IDisposable
{
    bool IsFrameInProgress { get; }
    bool IsCommandListOpen { get; }
    void BeginFrame();
    void EndFrame();
}

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

public interface IShaderManager : IDisposable
{
    bool IsInitialized { get; }
    void Initialize();
}

public interface ISpriteRenderer : IDisposable
{
    bool IsInitialized { get; }
    void Initialize(IGraphicsDevice device);
    void DrawSprite(Vector2 position, Vector2 size, Vector4 color);
    void FlushBatch();
}

public interface IRenderBatch<T> : IDisposable
{
    int Count { get; }
    void Add(T item);
    void Clear();
    void Render(IRenderCommandList commandList);
    void Initialize(ISpriteRenderer spriteRenderer);
}