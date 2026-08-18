using HEngine.Core.Primitives;

namespace HEngine.Core.Managers;

public sealed class EntityManager : IDisposable {
    private readonly Dictionary<uint, uint> _entityGenerations = new();
    private readonly Queue<uint> _freeEntityIds = new();
    private readonly HashSet<uint> _freeEntityIdSet = new();
    private readonly Lock _lock = new();
    private bool _disposed;
    private uint _nextEntityId = 1;

    public int ActiveEntityCount
    {
        get
        {
            lock (_lock)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                return (int)(_nextEntityId - 1 - _freeEntityIds.Count);
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        lock (_lock)
        {
            if (_disposed)
                return;

            _entityGenerations.Clear();
            _freeEntityIds.Clear();
            _freeEntityIdSet.Clear();
            _disposed = true;
        }
    }

    public Entity CreateEntity()
    {
        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            uint entityId;

            if (_freeEntityIds.Count > 0)
            {
                entityId = _freeEntityIds.Dequeue();
                _freeEntityIdSet.Remove(entityId);
                _entityGenerations[entityId]++;
            }
            else
            {
                entityId = _nextEntityId++;
                _entityGenerations[entityId] = 1;
            }

            return new Entity(entityId, _entityGenerations[entityId]);
        }
    }

    public void DestroyEntity(Entity entity)
    {
        lock (_lock)
        {
            if (_disposed || !IsEntityValid(entity))
                return;

            _freeEntityIds.Enqueue(entity.Id);
            _freeEntityIdSet.Add(entity.Id);
        }
    }

    public bool IsEntityValid(Entity entity)
    {
        lock (_lock)
        {
            if (_disposed)
                return false;

            return _entityGenerations.TryGetValue(entity.Id, out var generation) &&
                generation == entity.Generation &&
                !_freeEntityIdSet.Contains(entity.Id);
        }
    }

    public IEnumerable<Entity> GetAllActiveEntities()
    {
        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            return _entityGenerations
                   .Where(kvp => !_freeEntityIdSet.Contains(kvp.Key))
                   .Select(kvp => new Entity(kvp.Key, kvp.Value))
                   .ToList();
        }
    }

    public EntityManagerStats GetStats()
    {
        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            return new EntityManagerStats
            {
                ActiveCount = ActiveEntityCount,
                TotalCreated = _nextEntityId - 1,
                RecycledCount = _freeEntityIds.Count,
                NextEntityId = _nextEntityId,
                GenerationsCount = _entityGenerations.Count
            };
        }
    }

    public bool IsEntityActive(uint entityId)
    {
        lock (_lock)
        {
            if (_disposed)
                return false;

            return _entityGenerations.ContainsKey(entityId) &&
                !_freeEntityIdSet.Contains(entityId);
        }
    }

    public void ReserveCapacity(int expectedEntityCount)
    {
        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_entityGenerations.Count < expectedEntityCount)
                _entityGenerations.EnsureCapacity(expectedEntityCount);
        }
    }
}