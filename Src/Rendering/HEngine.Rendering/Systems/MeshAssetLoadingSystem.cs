using System.Collections.Concurrent;
using HEngine.Assets.Assets;
using HEngine.Core.Contracts;
using HEngine.Core.Managers;
using HEngine.Core.Primitives;
using HEngine.ECS.Queries;
using HEngine.Rendering.Assets;
using HEngine.Rendering.Components;

namespace HEngine.Rendering.Systems;

public class MeshAssetLoadingSystem : ISystem
{
    private WorldManager? _world;
    private AssetManager? _assetManager;
    private QueryBuilder? _queryBuilder;
    private readonly ConcurrentDictionary<Entity, Task> _loadingTasks = new();
    private readonly object _updateLock = new();
    private bool _disposed;

    public void Initialize(WorldManager world)
    {
        _world = world ?? throw new ArgumentNullException(nameof(world));
        _queryBuilder = new QueryBuilder(world.ComponentManager, world.EntityManager);
        _assetManager = new AssetManager(LoadMeshFile);
    }

    private static Task<LoadedMesh> LoadMeshFile(string path)
    {
        var (vertices, indices) = SimpleMeshFormat.Load(path);
        return Task.FromResult(new LoadedMesh(vertices, indices));
    }

    public void Initialize(WorldManager world, AssetManager assetManager)
    {
        _world = world ?? throw new ArgumentNullException(nameof(world));
        _assetManager = assetManager ?? throw new ArgumentNullException(nameof(assetManager));
        _queryBuilder = new QueryBuilder(world.ComponentManager, world.EntityManager);
    }

    public void Update(float deltaTime)
    {
        if (_disposed || _world == null || _queryBuilder == null || _assetManager == null)
            return;

        var query = _queryBuilder.With<MeshAsset>();

        foreach (var (entity, meshAsset) in query)
        {
            if (meshAsset.LoadState == AssetLoadState.NotLoaded && !_loadingTasks.ContainsKey(entity))
            {
                lock (_updateLock)
                {
                    if (_world.HasComponent<MeshAsset>(entity))
                    {
                        ref var asset = ref _world.GetComponent<MeshAsset>(entity);
                        asset.LoadState = AssetLoadState.Loading;
                    }
                }

                var task = LoadMeshAsync(entity, meshAsset.AssetPath);
                _loadingTasks[entity] = task;
            }
        }

        CleanupCompletedTasks();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        Task.WaitAll(_loadingTasks.Values.ToArray(), TimeSpan.FromSeconds(5));
        _loadingTasks.Clear();

        _assetManager?.Dispose();
        _world = null;
        _queryBuilder = null;
        _assetManager = null;
        _disposed = true;
    }

    private async Task LoadMeshAsync(Entity entity, string assetPath)
    {
        try
        {
            var mesh = await _assetManager!.LoadMeshAsync(assetPath);

            lock (_updateLock)
            {
                if (_world != null && _world.HasComponent<MeshAsset>(entity))
                {
                    ref var asset = ref _world.GetComponent<MeshAsset>(entity);
                    asset.Vertices = mesh.Vertices;
                    asset.Indices = mesh.Indices;
                    asset.LoadState = AssetLoadState.Loaded;
                    asset.ErrorMessage = null;
                }
            }
        }
        catch (Exception ex)
        {
            lock (_updateLock)
            {
                if (_world != null && _world.HasComponent<MeshAsset>(entity))
                {
                    ref var asset = ref _world.GetComponent<MeshAsset>(entity);
                    asset.LoadState = AssetLoadState.Failed;
                    asset.ErrorMessage = ex.Message;
                }
            }
        }
    }

    private void CleanupCompletedTasks()
    {
        var completedEntities = _loadingTasks
            .Where(kvp => kvp.Value.IsCompleted)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var entity in completedEntities)
        {
            _loadingTasks.TryRemove(entity, out _);
        }
    }
}
