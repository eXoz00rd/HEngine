using System.Diagnostics;

namespace HEngine.Rendering.Diagnostics;

public class RenderingMetrics
{
    private readonly Stopwatch _frameTimer = new();
    private readonly List<double> _frameTimes = new(120);
    private readonly object _lock = new();

    private int _drawCallCount;
    private int _batchFlushCount;
    private int _spriteCount;
    private int _vertexCount;
    private int _constantBufferUpdateCount;
    private int _constantBufferSkipCount;

    private int _totalFrames;
    private double _totalFrameTime;

    public void BeginFrame()
    {
        _frameTimer.Restart();
    }

    public void EndFrame()
    {
        _frameTimer.Stop();
        var frameTime = _frameTimer.Elapsed.TotalMilliseconds;

        lock (_lock)
        {
            _frameTimes.Add(frameTime);
            if (_frameTimes.Count > 120)
                _frameTimes.RemoveAt(0);

            _totalFrames++;
            _totalFrameTime += frameTime;
        }
    }

    public void IncrementDrawCalls(int count = 1)
    {
        Interlocked.Add(ref _drawCallCount, count);
    }

    public void IncrementBatchFlushes(int count = 1)
    {
        Interlocked.Add(ref _batchFlushCount, count);
    }

    public void IncrementSprites(int count)
    {
        Interlocked.Add(ref _spriteCount, count);
    }

    public void IncrementVertices(int count)
    {
        Interlocked.Add(ref _vertexCount, count);
    }

    public void IncrementConstantBufferUpdates(int count = 1)
    {
        Interlocked.Add(ref _constantBufferUpdateCount, count);
    }

    public void IncrementConstantBufferSkips(int count = 1)
    {
        Interlocked.Add(ref _constantBufferSkipCount, count);
    }

    public double GetAverageFrameTime()
    {
        lock (_lock)
        {
            return _frameTimes.Count > 0 ? _frameTimes.Average() : 0;
        }
    }

    public double GetMinFrameTime()
    {
        lock (_lock)
        {
            return _frameTimes.Count > 0 ? _frameTimes.Min() : 0;
        }
    }

    public double GetMaxFrameTime()
    {
        lock (_lock)
        {
            return _frameTimes.Count > 0 ? _frameTimes.Max() : 0;
        }
    }

    public double GetPercentile(double percentile)
    {
        lock (_lock)
        {
            if (_frameTimes.Count == 0) return 0;

            var sorted = _frameTimes.OrderBy(x => x).ToList();
            var index = (int)Math.Ceiling(percentile / 100.0 * sorted.Count) - 1;
            index = Math.Max(0, Math.Min(sorted.Count - 1, index));
            return sorted[index];
        }
    }

    public int GetDrawCallsPerSecond()
    {
        lock (_lock)
        {
            if (_totalFrameTime == 0) return 0;
            return (int)(_drawCallCount / (_totalFrameTime / 1000.0));
        }
    }

    public int GetBatchFlushesPerSecond()
    {
        lock (_lock)
        {
            if (_totalFrameTime == 0) return 0;
            return (int)(_batchFlushCount / (_totalFrameTime / 1000.0));
        }
    }

    public int GetSpritesPerSecond()
    {
        lock (_lock)
        {
            if (_totalFrameTime == 0) return 0;
            return (int)(_spriteCount / (_totalFrameTime / 1000.0));
        }
    }

    public MetricsSnapshot GetSnapshot()
    {
        lock (_lock)
        {
            return new MetricsSnapshot
            {
                TotalFrames = _totalFrames,
                AverageFrameTime = GetAverageFrameTime(),
                MinFrameTime = GetMinFrameTime(),
                MaxFrameTime = GetMaxFrameTime(),
                Percentile99 = GetPercentile(99),
                DrawCallCount = _drawCallCount,
                BatchFlushCount = _batchFlushCount,
                SpriteCount = _spriteCount,
                VertexCount = _vertexCount,
                ConstantBufferUpdateCount = _constantBufferUpdateCount,
                ConstantBufferSkipCount = _constantBufferSkipCount
            };
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _frameTimes.Clear();
            _drawCallCount = 0;
            _batchFlushCount = 0;
            _spriteCount = 0;
            _vertexCount = 0;
            _constantBufferUpdateCount = 0;
            _constantBufferSkipCount = 0;
            _totalFrames = 0;
            _totalFrameTime = 0;
        }
    }

    public string GetFormattedSummary()
    {
        var snapshot = GetSnapshot();
        var avgFps = snapshot.AverageFrameTime > 0 ? 1000.0 / snapshot.AverageFrameTime : 0;

        return $"Frame: {snapshot.AverageFrameTime:F2}ms ({avgFps:F1} FPS) | " +
               $"Min: {snapshot.MinFrameTime:F2}ms | Max: {snapshot.MaxFrameTime:F2}ms | " +
               $"99th: {snapshot.Percentile99:F2}ms | " +
               $"Batches: {snapshot.BatchFlushCount} | " +
               $"Sprites: {snapshot.SpriteCount} | " +
               $"DrawCalls: {snapshot.DrawCallCount} | " +
               $"CB Updates: {snapshot.ConstantBufferUpdateCount} (Skipped: {snapshot.ConstantBufferSkipCount})";
    }
}

public record MetricsSnapshot
{
    public int TotalFrames { get; init; }
    public double AverageFrameTime { get; init; }
    public double MinFrameTime { get; init; }
    public double MaxFrameTime { get; init; }
    public double Percentile99 { get; init; }
    public int DrawCallCount { get; init; }
    public int BatchFlushCount { get; init; }
    public int SpriteCount { get; init; }
    public int VertexCount { get; init; }
    public int ConstantBufferUpdateCount { get; init; }
    public int ConstantBufferSkipCount { get; init; }
}
