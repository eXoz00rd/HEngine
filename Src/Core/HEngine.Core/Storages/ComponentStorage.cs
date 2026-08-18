using HEngine.Core.Contracts;
using HEngine.Core.Primitives;
using System.Runtime.InteropServices;

namespace HEngine.Core.Storages;

internal sealed class ComponentStorage<T> : IComponentStorage<T>, IDisposable where T : struct, IComponent {
    private readonly Queue<uint> _freeIndices;
    private readonly ReaderWriterLockSlim _lock = new();
    private uint _capacity;
    private T[] _components;
    private uint _count;
    private uint[] _entityToIndex;
    private Entity[] _indexToEntity;

    public ComponentStorage(int initialCapacity = 1024)
    {
        _capacity = (uint)initialCapacity;
        _components = new T[_capacity];
        _entityToIndex = new uint[_capacity];
        _indexToEntity = new Entity[_capacity];
        _freeIndices = new Queue<uint>();
        
        Array.Fill(_entityToIndex, uint.MaxValue);
        Array.Fill(_indexToEntity, Entity.Null);
    }

    public uint Count
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                return _count;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }

    public Type ComponentType => typeof(T);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasComponent(Entity entity)
    {
        if (entity.Id >= _capacity)
            return false;

        _lock.EnterReadLock();
        try
        {
            return HasComponentUnsafe(entity);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T GetComponent(Entity entity)
    {
        _lock.EnterReadLock();
        try
        {
            if (!HasComponentUnsafe(entity))
                ThrowEntityNotFound(entity);

            return ref _components[_entityToIndex[entity.Id]];
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetComponent(Entity entity, out T component)
    {
        if (entity.Id >= _capacity)
        {
            component = default;
            return false;
        }

        _lock.EnterReadLock();
        try
        {
            if (HasComponentUnsafe(entity))
            {
                component = _components[_entityToIndex[entity.Id]];
                return true;
            }

            component = default;
            return false;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public ref T AddComponent(Entity entity, in T component)
    {
        _lock.EnterWriteLock();
        try
        {
            if (entity.Id < _capacity && HasComponentUnsafe(entity))
                ThrowComponentExists(entity);

            EnsureCapacity(entity.Id);

            var index = GetNextIndexUnsafe();
            _components[index] = component;
            _entityToIndex[entity.Id] = index;
            _indexToEntity[index] = entity;
            _count++;

            return ref _components[index];
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public bool RemoveComponent(Entity entity)
    {
        if (entity.Id >= _capacity)
            return false;

        _lock.EnterWriteLock();
        try
        {
            if (!HasComponentUnsafe(entity))
                return false;

            var index = _entityToIndex[entity.Id];
            _entityToIndex[entity.Id] = uint.MaxValue;
            _indexToEntity[index] = Entity.Null;
            _freeIndices.Enqueue(index);
            _count--;

            _components[index] = default;

            return true;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public ReadOnlySpan<T> GetAllComponents()
        => GetAllComponentsReadOnly();

    public void GetEntitiesWithComponent(List<Entity> entities)
    {
        _lock.EnterReadLock();
        try
        {
            entities.Clear();
            if (_count == 0)
                return;

            entities.EnsureCapacity((int)_count);

            for (uint i = 0; i < _capacity; i++)
            {
                if (_indexToEntity[i].IsValid)
                    entities.Add(_indexToEntity[i]);
            }
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public void Clear()
    {
        _lock.EnterWriteLock();
        try
        {
            Array.Fill(_entityToIndex, uint.MaxValue);
            Array.Fill(_indexToEntity, Entity.Null);
            Array.Clear(_components);
            _freeIndices.Clear();
            _count = 0;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void Compact()
    {
        _lock.EnterWriteLock();
        try
        {
            if (_count == 0)
                return;

            var newComponents = new T[_count];
            var newIndexToEntity = new Entity[_count];
            uint writeIndex = 0;
            
            for (uint i = 0; i < _capacity && writeIndex < _count; i++)
            {
                if (_indexToEntity[i].IsValid)
                {
                    newComponents[writeIndex] = _components[i];
                    newIndexToEntity[writeIndex] = _indexToEntity[i];
                    _entityToIndex[_indexToEntity[i].Id] = writeIndex;
                    writeIndex++;
                }
            }

            _components = newComponents;
            _indexToEntity = newIndexToEntity;
            _capacity = _count;
            _freeIndices.Clear();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
    
    public long GetMemoryUsage()
    {
        _lock.EnterReadLock();
        try
        {
            return GetMemoryUsageUnsafe();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public bool RemoveEntity(Entity entity)
        => RemoveComponent(entity);

    public bool HasEntity(Entity entity)
        => HasComponent(entity);

    public void GetAllEntities(List<Entity> entities)
        => GetEntitiesWithComponent(entities);

    public void Dispose()
        => _lock.Dispose();

    public ReadOnlySpan<T> GetAllComponentsReadOnly()
    {
        _lock.EnterReadLock();
        try
        {
            return GetDenseComponentsUnsafe();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public void TrimExcess()
    {
        _lock.EnterWriteLock();
        try
        {
            if (_count < _capacity / 2)
                Compact();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public ComponentStorageStats GetStats()
    {
        _lock.EnterReadLock();
        try
        {
            return new ComponentStorageStats
            {
                ComponentType = typeof(T),
                Count = _count,
                Capacity = _capacity,
                MemoryUsage = GetMemoryUsageUnsafe(),
                FreeSlots = (uint)_freeIndices.Count,
                Fragmentation = _count > 0 ?
                    (_capacity - _count) / (float)_capacity :
                    0f
            };
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private long GetMemoryUsageUnsafe()
    {
        var componentSize = Marshal.SizeOf<T>();
        var entitySize = Marshal.SizeOf<Entity>();
        const int uintSize = sizeof(uint);

        return _capacity * (componentSize + entitySize + uintSize) +
            _freeIndices.Count * sizeof(uint);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool HasComponentUnsafe(Entity entity)
        => _entityToIndex[entity.Id] != uint.MaxValue &&
            _indexToEntity[_entityToIndex[entity.Id]] == entity;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint GetNextIndexUnsafe()
        => _freeIndices.Count > 0 ?
            _freeIndices.Dequeue() :
            _count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureCapacity(uint entityId)
    {
        var requiredCapacity = Math.Max(entityId + 1, _count + 1);
        if (requiredCapacity <= _capacity)
            return;

        var newCapacity = Math.Max(_capacity * 2, requiredCapacity);
        Array.Resize(ref _components, (int)newCapacity);
        Array.Resize(ref _entityToIndex, (int)newCapacity);
        Array.Resize(ref _indexToEntity, (int)newCapacity);
        
        for (var i = _capacity; i < newCapacity; i++)
        {
            _entityToIndex[i] = uint.MaxValue;
            _indexToEntity[i] = Entity.Null;
        }

        _capacity = newCapacity;
    }

    private ReadOnlySpan<T> GetDenseComponentsUnsafe()
    {
        if (_count == 0)
            return ReadOnlySpan<T>.Empty;
        
        if (_freeIndices.Count == 0)
            return new ReadOnlySpan<T>(_components, 0, (int)_count);

        var endIndex = 0;
        for (var i = 0; i < _capacity && endIndex < _count; i++)
        {
            if (_indexToEntity[i].IsValid)
                endIndex = i + 1;
        }

        return new ReadOnlySpan<T>(_components, 0, endIndex);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowEntityNotFound(Entity entity)
        => throw new InvalidOperationException($"Entity {entity} does not have component {typeof(T).Name}");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowComponentExists(Entity entity)
        => throw new InvalidOperationException($"Entity {entity} already has component {typeof(T).Name}");
}

public struct ComponentStorageStats {
    public Type ComponentType;
    public uint Count;
    public uint Capacity;
    public long MemoryUsage;
    public uint FreeSlots;
    public float Fragmentation;
}