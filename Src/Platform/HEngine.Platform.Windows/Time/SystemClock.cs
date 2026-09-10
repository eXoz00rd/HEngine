using System.Diagnostics;
using HEngine.Platform.Time;

namespace HEngine.Platform.Windows.Time;

public sealed class SystemClock : IClock
{
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

    public TimeSpan Elapsed => _stopwatch.Elapsed;
}
