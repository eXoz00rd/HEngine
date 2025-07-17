using System.Diagnostics;

namespace HEngine.Core.Time;

public class GameTime {
    private readonly Stopwatch _stopwatch = new();
    private float _fpsTimer;
    private TimeSpan _lastFrameTime;
    private TimeSpan _totalGameTime;

    public GameTime()
    {
        _stopwatch.Start();
        _lastFrameTime = _stopwatch.Elapsed;
    }

    public float DeltaTime { get; private set; }

    public TimeSpan TotalGameTime => _totalGameTime;
    public float TotalSeconds => (float)_totalGameTime.TotalSeconds;
    public int FrameCount { get; private set; }

    public float FPS { get; private set; }

    public void Update()
    {
        var currentTime = _stopwatch.Elapsed;
        var elapsed = currentTime - _lastFrameTime;

        DeltaTime = (float)elapsed.TotalSeconds;
        _totalGameTime = currentTime;
        _lastFrameTime = currentTime;
        FrameCount++;

        _fpsTimer += DeltaTime;
        if (_fpsTimer >= 1.0f)
        {
            FPS = FrameCount / _fpsTimer;
            FrameCount = 0;
            _fpsTimer = 0.0f;
        }

        if (DeltaTime > 0.1f)
            DeltaTime = 0.016f;
    }

    public void Reset()
    {
        _stopwatch.Restart();
        _lastFrameTime = TimeSpan.Zero;
        _totalGameTime = TimeSpan.Zero;
        DeltaTime = 0.0f;
        FrameCount = 0;
        _fpsTimer = 0.0f;
        FPS = 0.0f;
    }
}