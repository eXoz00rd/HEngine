namespace HEngine.Core.Rendering.Contracts;

public interface IShaderManager : IDisposable
{
    bool IsInitialized { get; }
    void Initialize();
}