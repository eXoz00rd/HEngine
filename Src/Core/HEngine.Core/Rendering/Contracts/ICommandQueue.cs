namespace HEngine.Core.Rendering.Contracts;

public interface ICommandQueue : IDisposable
{
    bool IsFrameInProgress { get; }
    bool IsCommandListOpen { get; }
    void BeginFrame();
    void EndFrame();
}