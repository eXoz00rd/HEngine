using System.Collections.Concurrent;

namespace HEngine.Assets.Assets;

public class AssetManager : IDisposable
{
    private readonly ConcurrentDictionary<AssetId, CachedAsset> _loadedAssets = new();
    private readonly Dictionary<AssetId, PendingLoad> _pendingLoads = new();
    private readonly Dictionary<AssetId, string> _importedPaths = new();
    private readonly Dictionary<string, AssetId> _idsByNormalizedPath = new(StringComparer.Ordinal);
    private readonly object _importLock = new();
    private readonly object _cacheLock = new();
    private readonly Func<string, Task<LoadedMesh>> _meshLoader;
    private bool _disposed;
    private int _generation;

    public AssetManager(Func<string, Task<LoadedMesh>> meshLoader)
    {
        _meshLoader = meshLoader ?? throw new ArgumentNullException(nameof(meshLoader));
    }

    public int LoadedAssetCount => _loadedAssets.Count;

    public void Dispose()
    {
        lock (_cacheLock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            UnloadAllUnsynchronized();
        }
    }

    public AssetId Import(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path cannot be null or empty", nameof(path));
        }

        lock (_cacheLock)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(AssetManager));
            }
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
        Lazy<Task<object>> lazyLoad;
        lock (_cacheLock)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(AssetManager));
            }

            if (_loadedAssets.TryGetValue(id, out var cached))
            {
                cached.IncrementRefCount();
                return (LoadedMesh)cached.Asset;
            }

            if (_pendingLoads.TryGetValue(id, out var pending))
            {
                pending.AttachedCount++;
                lazyLoad = pending.Lazy;
            }
            else
            {
                pending = new PendingLoad(_generation);
                pending.Lazy = new Lazy<Task<object>>(() => LoadAssetInternalAsync(id, pending));
                _pendingLoads[id] = pending;
                lazyLoad = pending.Lazy;
            }
        }

        return (LoadedMesh)await lazyLoad.Value;
    }

    public void Unload(AssetId id)
    {
        lock (_cacheLock)
        {
            if (_loadedAssets.TryGetValue(id, out var cached))
            {
                if (cached.DecrementRefCount() > 0)
                {
                    return;
                }

                _loadedAssets.TryRemove(id, out _);
                (cached.Asset as IDisposable)?.Dispose();
                return;
            }

            if (_pendingLoads.TryGetValue(id, out var pending) && pending.AttachedCount > 0)
            {
                pending.AttachedCount--;
            }
        }
    }

    public void UnloadAll()
    {
        lock (_cacheLock)
        {
            UnloadAllUnsynchronized();
        }
    }

    private void UnloadAllUnsynchronized()
    {
        _generation++;
        _pendingLoads.Clear();

        foreach (var kvp in _loadedAssets)
        {
            (kvp.Value.Asset as IDisposable)?.Dispose();
        }

        _loadedAssets.Clear();
    }

    public bool IsLoaded(AssetId id) => _loadedAssets.ContainsKey(id);

    public bool IsLoading(AssetId id)
    {
        lock (_cacheLock)
        {
            return _pendingLoads.ContainsKey(id);
        }
    }

    public int GetRefCount(AssetId id) => _loadedAssets.TryGetValue(id, out var cached) ? cached.RefCount : 0;

    private async Task<object> LoadAssetInternalAsync(AssetId id, PendingLoad pending)
    {
        try
        {
            var path = ResolvePath(id);
            var asset = await Task.Run(() => _meshLoader(path));

            lock (_cacheLock)
            {
                RemoveOwnedPendingLoad(id, pending);

                if (_disposed || pending.Generation != _generation || pending.AttachedCount <= 0)
                {
                    (asset as IDisposable)?.Dispose();
                    return asset;
                }

                var cached = new CachedAsset(asset);
                for (var i = 0; i < pending.AttachedCount; i++)
                {
                    cached.IncrementRefCount();
                }

                _loadedAssets[id] = cached;
            }

            return asset;
        }
        catch
        {
            lock (_cacheLock)
            {
                RemoveOwnedPendingLoad(id, pending);
            }

            throw;
        }
    }

    private void RemoveOwnedPendingLoad(AssetId id, PendingLoad pending)
    {
        if (_pendingLoads.TryGetValue(id, out var current) && ReferenceEquals(current, pending))
        {
            _pendingLoads.Remove(id);
        }
    }

    private static string NormalizeKey(string absolutePath)
    {
        return absolutePath.ToLowerInvariant();
    }

    private sealed class PendingLoad
    {
        public PendingLoad(int generation)
        {
            Generation = generation;
            AttachedCount = 1;
        }

        public Lazy<Task<object>> Lazy { get; set; } = null!;

        public int Generation { get; }

        public int AttachedCount { get; set; }
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
