using HEngine.Core.Rendering.DirectX12.Contracts;

namespace HEngine.Core.Rendering.Contracts;

public interface IRenderBatch<T> : IDisposable
{
    int Count { get; }
    void Add(T item);
    void Clear();
    void Render(IRenderCommandList commandList);
    void Initialize(ISpriteRenderer spriteRenderer);
}