using System.Collections.Concurrent;

namespace HEngine.Assets.Assets;

public class AssetManager : IDisposable
{
    private readonly ConcurrentDictionary<string, CachedAsset> _loadedAssets = new();
    private readonly ConcurrentDictionary<string, Task<object>> _loadingTasks = new();
    private readonly Func<string, Task<LoadedMesh>> _meshLoader;
    private bool _disposed;

    public AssetManager(Func<string, Task<LoadedMesh>> meshLoader)
    {
        _meshLoader = meshLoader ?? throw new ArgumentNullException(nameof(meshLoader));
    }

    public int LoadedAssetCount => _loadedAssets.Count;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        UnloadAll();
        _disposed = true;
    }

    public async Task<LoadedMesh> LoadMeshAsync(string path)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(AssetManager));
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path cannot be null or empty", nameof(path));
        }

        path = NormalizePath(path);

        if (_loadedAssets.TryGetValue(path, out var cached))
        {
            cached.IncrementRefCount();
            return (LoadedMesh)cached.Asset;
        }

        var loadingTask = _loadingTasks.GetOrAdd(path, _ => LoadAssetInternalAsync(path));

        try
        {
            var asset = await loadingTask;
            return (LoadedMesh)asset;
        }
        finally
        {
            _loadingTasks.TryRemove(path, out _);
        }
    }

    public void Unload(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        path = NormalizePath(path);

        if (!_loadedAssets.TryGetValue(path, out var cached))
        {
            return;
        }

        if (cached.DecrementRefCount() > 0)
        {
            return;
        }

        _loadedAssets.TryRemove(path, out _);
        (cached.Asset as IDisposable)?.Dispose();
    }

    public void UnloadAll()
    {
        foreach (var kvp in _loadedAssets)
        {
            (kvp.Value.Asset as IDisposable)?.Dispose();
        }

        _loadedAssets.Clear();
    }

    public bool IsLoaded(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        path = NormalizePath(path);
        return _loadedAssets.ContainsKey(path);
    }

    public bool IsLoading(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        path = NormalizePath(path);
        return _loadingTasks.ContainsKey(path);
    }

    public int GetRefCount(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return 0;
        }

        path = NormalizePath(path);
        return _loadedAssets.TryGetValue(path, out var cached) ? cached.RefCount : 0;
    }

    private async Task<object> LoadAssetInternalAsync(string path)
    {
        var asset = await Task.Run(() => _meshLoader(path));

        var cached = new CachedAsset(asset);
        cached.IncrementRefCount();
        _loadedAssets[path] = cached;

        return asset;
    }

    private static string NormalizePath(string path)
    {
        return Path.GetFullPath(path).ToLowerInvariant();
    }

    private class CachedAsset
    {
        private readonly object _lock = new();
        private int _refCount;

        public CachedAsset(object asset)
        {
            Asset = asset ?? throw new ArgumentNullException(nameof(asset));
            _refCount = 0;
        }

        public object Asset { get; }

        public int RefCount
        {
            get
            {
                lock (_lock)
                {
                    return _refCount;
                }
            }
        }

        public int IncrementRefCount()
        {
            lock (_lock)
            {
                return ++_refCount;
            }
        }

        public int DecrementRefCount()
        {
            lock (_lock)
            {
                if (_refCount > 0)
                {
                    _refCount--;
                }

                return _refCount;
            }
        }
    }
}