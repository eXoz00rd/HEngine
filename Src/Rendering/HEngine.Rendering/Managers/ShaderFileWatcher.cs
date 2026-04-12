using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace HEngine.Rendering.Managers;

public sealed class ShaderFileWatcher : IDisposable
{
    private readonly FileSystemWatcher _watcher;
    private readonly ConcurrentDictionary<string, Timer> _debounceTimers;
    private readonly TimeSpan _debounceDelay;
    private bool _disposed;

    public event Action<string>? ShaderFileChanged;

    public ShaderFileWatcher(string shaderDirectory, TimeSpan? debounceDelay = null)
    {
        if (!Directory.Exists(shaderDirectory))
        {
            throw new DirectoryNotFoundException($"Shader directory not found: {shaderDirectory}");
        }

        _debounceDelay = debounceDelay ?? TimeSpan.FromMilliseconds(500);
        _debounceTimers = new ConcurrentDictionary<string, Timer>();

        _watcher = new FileSystemWatcher(shaderDirectory)
        {
            Filter = "*.hlsl",
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
            EnableRaisingEvents = true
        };

        _watcher.Changed += OnFileChanged;
        _watcher.Created += OnFileChanged;
        _watcher.Renamed += OnFileRenamed;
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        if (_disposed)
            return;

        var fileName = Path.GetFileName(e.FullPath);
        DebounceFileChange(fileName);
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        if (_disposed)
            return;

        var fileName = Path.GetFileName(e.FullPath);
        DebounceFileChange(fileName);
    }

    private void DebounceFileChange(string fileName)
    {
        if (_debounceTimers.TryGetValue(fileName, out var existingTimer))
        {
            existingTimer.Change(_debounceDelay, Timeout.InfiniteTimeSpan);
        }
        else
        {
            var timer = new Timer(_ =>
            {
                if (_debounceTimers.TryRemove(fileName, out var timerToDispose))
                {
                    timerToDispose.Dispose();
                }

                if (!_disposed)
                {
                    ShaderFileChanged?.Invoke(fileName);
                }
            }, null, _debounceDelay, Timeout.InfiniteTimeSpan);

            _debounceTimers.TryAdd(fileName, timer);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        _watcher.EnableRaisingEvents = false;
        _watcher.Changed -= OnFileChanged;
        _watcher.Created -= OnFileChanged;
        _watcher.Renamed -= OnFileRenamed;
        _watcher.Dispose();

        foreach (var timer in _debounceTimers.Values)
        {
            timer.Dispose();
        }
        _debounceTimers.Clear();
    }
}
