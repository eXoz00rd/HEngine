using System.Numerics;

namespace HEngine.Core.Rendering.DirectX12.Contracts;

public interface IDirectX12Device : IDisposable
{
    bool IsInitialized { get; }
    bool ShouldClose { get; }
    void Initialize(int width, int height, string title);
    void BeginFrame();
    void EndFrame();
    void Clear(Vector4 clearColor);
    void Present();
    DirectX12CommandQueue GetCommandQueue();
}