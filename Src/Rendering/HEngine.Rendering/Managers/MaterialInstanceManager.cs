using HEngine.Rendering.Data;

namespace HEngine.Rendering.Managers;

public sealed class MaterialInstanceManager
{
    private readonly Dictionary<Material, List<MaterialInstance>> _instancesByMaterial = new();
    private readonly Dictionary<MaterialInstance, int> _instanceIds = new();
    private int _nextInstanceId;

    public IReadOnlyDictionary<Material, List<MaterialInstance>> InstancesByMaterial => _instancesByMaterial;
    public int TotalInstanceCount => _instanceIds.Count;

    public MaterialInstance CreateInstance(Material baseMaterial)
    {
        ArgumentNullException.ThrowIfNull(baseMaterial);

        var instance = baseMaterial.CreateInstance();
        RegisterInstance(baseMaterial, instance);
        return instance;
    }

    public void RegisterInstance(Material baseMaterial, MaterialInstance instance)
    {
        ArgumentNullException.ThrowIfNull(baseMaterial);
        ArgumentNullException.ThrowIfNull(instance);

        if (!_instancesByMaterial.ContainsKey(baseMaterial))
        {
            _instancesByMaterial[baseMaterial] = new List<MaterialInstance>();
        }

        if (!_instancesByMaterial[baseMaterial].Contains(instance))
        {
            _instancesByMaterial[baseMaterial].Add(instance);
            _instanceIds[instance] = _nextInstanceId++;
        }
    }

    public void UnregisterInstance(MaterialInstance instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        var baseMaterial = instance.BaseMaterial;
        if (_instancesByMaterial.TryGetValue(baseMaterial, out var instances))
        {
            instances.Remove(instance);
            if (instances.Count == 0)
            {
                _instancesByMaterial.Remove(baseMaterial);
            }
        }

        _instanceIds.Remove(instance);
    }

    public bool TryGetInstanceId(MaterialInstance instance, out int id)
    {
        return _instanceIds.TryGetValue(instance, out id);
    }

    public int GetInstanceId(MaterialInstance instance)
    {
        if (_instanceIds.TryGetValue(instance, out var id))
            return id;

        throw new ArgumentException("Instance not registered", nameof(instance));
    }

    public IEnumerable<MaterialInstance> GetInstances(Material baseMaterial)
    {
        if (_instancesByMaterial.TryGetValue(baseMaterial, out var instances))
            return instances;

        return Enumerable.Empty<MaterialInstance>();
    }

    public int GetInstanceCount(Material baseMaterial)
    {
        if (_instancesByMaterial.TryGetValue(baseMaterial, out var instances))
            return instances.Count;

        return 0;
    }

    public bool HasInstances(Material baseMaterial)
    {
        return _instancesByMaterial.ContainsKey(baseMaterial) &&
               _instancesByMaterial[baseMaterial].Count > 0;
    }

    public MaterialInstanceData[] GetInstanceDataBatch(Material baseMaterial)
    {
        if (!_instancesByMaterial.TryGetValue(baseMaterial, out var instances))
            return Array.Empty<MaterialInstanceData>();

        var data = new MaterialInstanceData[instances.Count];
        for (int i = 0; i < instances.Count; i++)
        {
            data[i] = MaterialInstanceData.FromMaterialInstance(instances[i]);
        }

        return data;
    }

    public void Clear()
    {
        _instancesByMaterial.Clear();
        _instanceIds.Clear();
        _nextInstanceId = 0;
    }

    public void ClearMaterial(Material baseMaterial)
    {
        if (_instancesByMaterial.TryGetValue(baseMaterial, out var instances))
        {
            foreach (var instance in instances)
            {
                _instanceIds.Remove(instance);
            }
            _instancesByMaterial.Remove(baseMaterial);
        }
    }

    public Dictionary<Material, MaterialInstanceData[]> GetAllInstanceDataBatches()
    {
        var result = new Dictionary<Material, MaterialInstanceData[]>();

        foreach (var kvp in _instancesByMaterial)
        {
            result[kvp.Key] = GetInstanceDataBatch(kvp.Key);
        }

        return result;
    }
}
