namespace HEngine.Runtime.Contracts;

public interface IGameLoop
{
    bool IsRunning { get; }
    void Run();
    void Stop();
}