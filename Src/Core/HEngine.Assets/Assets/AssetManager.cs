using System.Collections.Concurrent;

namespace HEngine.Assets.Assets;

public class AssetManager : IDisposable
{
    private readonly ConcurrentDictionary<AssetId, CachedAsset> _loadedAssets = new();
    private readonly ConcurrentDictionary<AssetId, Task<object>> _loadingTasks = new();
    private readonly Dictionary<AssetId, string> _importedPaths = new();
    private readonly Dictionary<string, AssetId> _idsByNormalizedPath = new(StringComparer.Ordinal);
    private readonly object _importLock = new();
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

    public AssetId Import(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path cannot be null or empty", nameof(path));
        }

        var absolutePath = Path.GetFullPath(path);
        var key = NormalizeKey(absolutePath);

        lock (_importLock)
        {
            if (_idsByNormalizedPath.TryGetValue(key, out var existingId))
            {
                return existingId;
            }

            var id = AssetId.New();
            _importedPaths[id] = absolutePath;
            _idsByNormalizedPath[key] = id;
            return id;
        }
    }

    public void Move(AssetId id, string newPath)
    {
        if (string.IsNullOrWhiteSpace(newPath))
        {
            throw new ArgumentException("Path cannot be null or empty", nameof(newPath));
        }

        var newAbsolutePath = Path.GetFullPath(newPath);
        var newKey = NormalizeKey(newAbsolutePath);

        lock (_importLock)
        {
            if (!_importedPaths.TryGetValue(id, out var oldPath))
            {
                throw new KeyNotFoundException($"Asset id '{id}' has not been imported.");
            }

            if (_idsByNormalizedPath.TryGetValue(newKey, out var owner) && !owner.Equals(id))
            {
                throw new InvalidOperationException(
                    $"Cannot move asset '{id}' to '{newPath}': that path is already owned by asset '{owner}'.");
            }

            _idsByNormalizedPath.Remove(NormalizeKey(oldPath));
            _importedPaths[id] = newAbsolutePath;
            _idsByNormalizedPath[newKey] = id;
        }
    }

    public string ResolvePath(AssetId id)
    {
        lock (_importLock)
        {
            if (!_importedPaths.TryGetValue(id, out var path))
            {
                throw new KeyNotFoundException($"Asset id '{id}' has not been imported.");
            }

            return path;
        }
    }

    public async Task<LoadedMesh> LoadMeshAsync(AssetId id)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(AssetManager));
        }

        var path = ResolvePath(id);

        if (_loadedAssets.TryGetValue(id, out var cached))
        {
            cached.IncrementRefCount();
            return (LoadedMesh)cached.Asset;
        }

        var loadingTask = _loadingTasks.GetOrAdd(id, _ => LoadAssetInternalAsync(id, path));

        try
        {
            var asset = await loadingTask;
            return (LoadedMesh)asset;
        }
        finally
        {
            _loadingTasks.TryRemove(id, out _);
        }
    }

    public void Unload(AssetId id)
    {
        if (!_loadedAssets.TryGetValue(id, out var cached))
        {
            return;
        }

        if (cached.DecrementRefCount() > 0)
        {
            return;
        }

        _loadedAssets.TryRemove(id, out _);
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

    public bool IsLoaded(AssetId id) => _loadedAssets.ContainsKey(id);

    public bool IsLoading(AssetId id) => _loadingTasks.ContainsKey(id);

    public int GetRefCount(AssetId id) => _loadedAssets.TryGetValue(id, out var cached) ? cached.RefCount : 0;

    private async Task<object> LoadAssetInternalAsync(AssetId id, string path)
    {
        var asset = await Task.Run(() => _meshLoader(path));

        var cached = new CachedAsset(asset);
        cached.IncrementRefCount();
        _loadedAssets[id] = cached;

        return asset;
    }

    private static string NormalizeKey(string absolutePath)
    {
        return absolutePath.ToLowerInvariant();
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
