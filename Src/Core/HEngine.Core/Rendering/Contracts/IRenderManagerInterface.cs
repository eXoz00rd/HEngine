// Src/Core/HEngine.Core/Rendering/Contracts/IRenderManager.cs
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
}

// Src/Core/HEngine.Core/Rendering/Contracts/IRenderingSystem.cs
namespace HEngine.Core.Rendering.Contracts;

public interface IRenderingSystem : IDisposable
{
    bool IsInitialized { get; }
    void Initialize();
    void Update(float deltaTime);
    void Render(IRenderContext context);
}

// Src/Core/HEngine.Core/Rendering/Contracts/IGraphicsDevice.cs
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

// Src/Core/HEngine.Core/Rendering/Contracts/ICommandQueue.cs
namespace HEngine.Core.Rendering.Contracts;

public interface ICommandQueue : IDisposable
{
    bool IsFrameInProgress { get; }
    bool IsCommandListOpen { get; }
    void BeginFrame();
    void EndFrame();
}

// Src/Core/HEngine.Core/Rendering/Contracts/ISpriteRenderer.cs
using System.Numerics;

namespace HEngine.Core.Rendering.Contracts;

public interface ISpriteRenderer : IDisposable
{
    void Initialize(IGraphicsDevice device);
    void DrawSprite(Vector2 position, Vector2 size, Vector4 color);
    void FlushBatch();
    bool IsInitialized { get; }
}

// Src/Core/HEngine.Core/Rendering/Contracts/IShaderManager.cs
namespace HEngine.Core.Rendering.Contracts;

public interface IShaderManager : IDisposable
{
    void Initialize();
    bool IsInitialized { get; }
}

// Src/Core/HEngine.Core/Rendering/Contracts/IRenderBatch.cs
namespace HEngine.Core.Rendering.Contracts;

public interface IRenderBatch<T> : IDisposable
{
    void Add(T item);
    void Clear();
    void Render(IRenderCommandList commandList);
    void Initialize(ISpriteRenderer spriteRenderer);
    int Count { get; }
}
