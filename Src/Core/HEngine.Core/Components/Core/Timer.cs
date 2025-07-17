using HEngine.Core.Contracts;

namespace HEngine.Core.Components.Core;


public struct Timer : IComponent {
    public float Duration;
    public float Elapsed;
    public bool IsRepeating;
    public bool IsActive;
    public bool AutoDestroy;

    public Timer(float duration, bool repeating = false, bool autoDestroy = false)
    {
        Duration = duration;
        Elapsed = 0f;
        IsRepeating = repeating;
        IsActive = true;
        AutoDestroy = autoDestroy;
    }

    public static Timer CreateLifetime(float duration)
        => new(duration, false, true);

    public float Progress => Duration > 0 ?
        Math.Clamp(Elapsed / Duration, 0f, 1f) :
        1f;

    public bool IsCompleted => Elapsed >= Duration;
    public bool ShouldDestroy => AutoDestroy && IsCompleted;

    public void Update(float deltaTime)
    {
        if (IsActive)
        {
            Elapsed += deltaTime;
            if (IsCompleted && IsRepeating)
                Elapsed = 0f;
        }
    }
}