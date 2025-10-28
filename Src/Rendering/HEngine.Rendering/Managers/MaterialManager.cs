using System.Collections.Generic;
using HEngine.Rendering.Data;

namespace HEngine.Rendering.Managers;

public class MaterialManager
{
    private readonly Dictionary<string, Material> _materials = new();
    private readonly object _lock = new();

    public Material GetOrCreate(string name)
    {
        lock (_lock)
        {
            if (!_materials.TryGetValue(name, out var material))
            {
                material = new Material();
                _materials[name] = material;
            }
            return material;
        }
    }

    public void Register(string name, Material material)
    {
        lock (_lock)
        {
            _materials[name] = material;
        }
    }

    public bool TryGet(string name, out Material? material)
    {
        lock (_lock)
        {
            return _materials.TryGetValue(name, out material);
        }
    }

    public void Remove(string name)
    {
        lock (_lock)
        {
            _materials.Remove(name);
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _materials.Clear();
        }
    }

    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _materials.Count;
            }
        }
    }
}
