namespace HEngine.Rendering.PostProcessing;

/// <summary>
/// Manages an ordered list of post-process effects applied via ping-pong render targets.
/// Effects are executed in ascending Order, reading from source and writing to destination each pass.
/// </summary>
public sealed class PostProcessStack
{
    private readonly List<IPostProcessEffect> _effects = new();
    private readonly PingPongRenderTargets _pingPong = new();

    public IReadOnlyList<IPostProcessEffect> Effects => _effects;
    public PingPongRenderTargets PingPong => _pingPong;

    public void AddEffect(IPostProcessEffect effect)
    {
        ArgumentNullException.ThrowIfNull(effect);
        _effects.Add(effect);
        _effects.Sort(static (a, b) => a.Order.CompareTo(b.Order));
    }

    public bool RemoveEffect(IPostProcessEffect effect)
    {
        return _effects.Remove(effect);
    }

    public void RemoveEffect(string name)
    {
        var toRemove = _effects.Find(e => e.Name == name);
        if (toRemove is not null)
            _effects.Remove(toRemove);
    }

    public T? GetEffect<T>() where T : class, IPostProcessEffect
    {
        return _effects.OfType<T>().FirstOrDefault();
    }

    public void Execute(IPostProcessCommandContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _pingPong.Reset();

        foreach (var effect in _effects)
        {
            if (!effect.IsEnabled)
                continue;

            effect.Execute(context);
            _pingPong.Flip();
        }
    }

    public int EnabledEffectCount => _effects.Count(e => e.IsEnabled);
}

