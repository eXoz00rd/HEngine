namespace HEngine.Core.Contracts;

public interface IGameLoop
{
    bool IsRunning { get; }
    void Run();
    void Stop();
}