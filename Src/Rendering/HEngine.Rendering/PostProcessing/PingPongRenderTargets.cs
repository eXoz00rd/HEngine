namespace HEngine.Rendering.PostProcessing;

/// <summary>
/// Tracks two HDR render targets (A and B) for ping-pong effect chaining.
/// Each effect reads from the current source and writes to the current destination,
/// then Flip() swaps them for the next effect.
/// </summary>
public sealed class PingPongRenderTargets
{
    private int _currentSource;
    private int _currentDestination;
    private int _flipCount;

    public const int RenderTargetA = 0;
    public const int RenderTargetB = 1;

    public PingPongRenderTargets()
    {
        _currentSource = RenderTargetA;
        _currentDestination = RenderTargetB;
    }

    public int CurrentSource => _currentSource;
    public int CurrentDestination => _currentDestination;
    public int FlipCount => _flipCount;

    public void Flip()
    {
        (_currentSource, _currentDestination) = (_currentDestination, _currentSource);
        _flipCount++;
    }

    public void Reset()
    {
        _currentSource = RenderTargetA;
        _currentDestination = RenderTargetB;
        _flipCount = 0;
    }
}

