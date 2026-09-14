using System.Numerics;
using HEngine.Rendering.DirectX12;

namespace HEngine.Rendering.Contracts;

public interface IDirectX12Device : IDisposable
{
    bool IsInitialized { get; }
    bool ShouldClose { get; }
    void Initialize(int width, int height, string title);
    void BeginFrame();
    void EndFrame();
    void Clear(Vector4 clearColor);
    void Present();
    int GetCurrentFrameIndex();
    DirectX12CommandQueue GetCommandQueue();
}