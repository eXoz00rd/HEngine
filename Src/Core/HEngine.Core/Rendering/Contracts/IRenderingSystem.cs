namespace HEngine.Core.Rendering.Contracts;

public interface IRenderingSystem : IDisposable
{
    bool IsInitialized { get; }
    void Initialize();
    void Update(float deltaTime);
    void Render(IRenderContext context);
}