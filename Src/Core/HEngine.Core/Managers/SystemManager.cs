using HEngine.Core.Contracts;

namespace HEngine.Core.Managers;

public sealed class SystemManager : IDisposable {
    private readonly List<SystemEntry> _activeSystems = [];
    private readonly Dictionary<Type, int> _systemIndices = new();
    private readonly List<SystemEntry> _systems = [];
    private bool _activeSystemsCacheValid;
    private bool _needsReordering;

    public void Dispose()
    {
        foreach (var entry in _systems)
            entry.System.Dispose();
        _systems.Clear();
        _systemIndices.Clear();
        _activeSystems.Clear();
    }

    public void AddSystem<T>(T system, int priority = 0, bool enabled = true) where T : ISystem
    {
        var systemType = typeof(T);

        if (_systemIndices.ContainsKey(systemType))
            throw new InvalidOperationException($"System of type {systemType.Name} already exists");

        var entry = new SystemEntry
        {
            System = system,
            Priority = priority,
            Enabled = enabled,
            SystemType = systemType
        };

        _systems.Add(entry);
        _systemIndices[systemType] = _systems.Count - 1;
        _needsReordering = true;
        _activeSystemsCacheValid = false;
    }

    public void RemoveSystem<T>() where T : ISystem
    {
        var systemType = typeof(T);

        if (!_systemIndices.TryGetValue(systemType, out var index))
            return;

        _systems[index].System.Dispose();
        _systems.RemoveAt(index);
        _systemIndices.Remove(systemType);

        for (var i = index; i < _systems.Count; i++)
            _systemIndices[_systems[i].SystemType] = i;

        _activeSystemsCacheValid = false;
    }

    public void SetSystemEnabled<T>(bool enabled) where T : ISystem
    {
        var systemType = typeof(T);

        if (!_systemIndices.TryGetValue(systemType, out var index))
            return;

        var entry = _systems[index];
        if (entry.Enabled != enabled)
        {
            entry.Enabled = enabled;
            _systems[index] = entry;
            _activeSystemsCacheValid = false;
        }
    }

    public void Update(float deltaTime)
    {
        if (_needsReordering)
        {
            SortSystemsByPriority();
            _needsReordering = false;
            _activeSystemsCacheValid = false;
        }

        if (!_activeSystemsCacheValid)
        {
            RebuildActiveSystemsCache();
            _activeSystemsCacheValid = true;
        }

        foreach (var entry in _activeSystems)
        {
            try
            {
                entry.System.Update(deltaTime);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in system {entry.SystemType.Name}: {ex.Message}");
            }
        }
    }

    private void SortSystemsByPriority()
    {
        _systems.Sort((a, b) =>
            {
                var priorityComparison = b.Priority.CompareTo(a.Priority); // Zamienione miejscami a i b
                return priorityComparison != 0 ?
                    priorityComparison :
                    string.Compare(a.SystemType.Name, b.SystemType.Name, StringComparison.Ordinal);
            }
        );

        for (var i = 0; i < _systems.Count; i++)
            _systemIndices[_systems[i].SystemType] = i;
    }

    private void RebuildActiveSystemsCache()
    {
        _activeSystems.Clear();
        foreach (var entry in _systems)
        {
            if (entry.Enabled)
                _activeSystems.Add(entry);
        }
    }

    public int GetSystemCount()
        => _systems.Count;

    public int GetActiveSystemCount()
    {
        if (_activeSystemsCacheValid)
            return _activeSystems.Count;

        RebuildActiveSystemsCache();
        _activeSystemsCacheValid = true;
        return _activeSystems.Count;
    }


    public IReadOnlyList<string> GetSystemNames()
        => _systems.Select(s => s.SystemType.Name).ToList();

    private struct SystemEntry {
        public ISystem System;
        public int Priority;
        public bool Enabled;
        public Type SystemType;
    }
}