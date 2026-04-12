using System.Collections.Generic;
using HEngine.Core.Rendering.Contracts;
using HEngine.Rendering.Data;
using HEngine.Rendering.Enums;

namespace HEngine.Rendering.Managers;

public class MaterialManager
{
    private readonly Dictionary<string, Material> _materials = new();
    private readonly Dictionary<string, MaterialTextureBindings> _textureBindings = new();
    private readonly object _lock = new();

    // ...existing GetOrCreate, Register, TryGet, Remove, Clear, Count...

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
            _textureBindings.Remove(name);
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _materials.Clear();
            _textureBindings.Clear();
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

    /// <summary>
    /// Resolves texture paths in a material's property block to GPU texture handles
    /// using the provided ITextureManager. Caches the result.
    /// </summary>
    public MaterialTextureBindings ResolveTextureBindings(string materialName, ITextureManager textureManager)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(materialName);
        ArgumentNullException.ThrowIfNull(textureManager);

        lock (_lock)
        {
            if (_textureBindings.TryGetValue(materialName, out var existing))
                return existing;

            if (!_materials.TryGetValue(materialName, out var material))
                throw new KeyNotFoundException($"Material '{materialName}' not found.");

            var bindings = new MaterialTextureBindings();
            bindings.ResolveFromPropertyBlock(material.PropertyBlock, path =>
            {
                var handle = textureManager.LoadTexture(path);
                return handle;
            });

            _textureBindings[materialName] = bindings;
            return bindings;
        }
    }

    /// <summary>
    /// Gets texture bindings for a material. Returns null if not resolved yet.
    /// </summary>
    public MaterialTextureBindings? GetTextureBindings(string materialName)
    {
        lock (_lock)
        {
            return _textureBindings.TryGetValue(materialName, out var bindings) ? bindings : null;
        }
    }

    /// <summary>
    /// Resolves a material's texture bindings with fallbacks to default textures.
    /// Returns the texture handle for the given slot, or the appropriate default.
    /// </summary>
    public int GetTextureHandleForSlot(
        string materialName,
        TextureSlot slot,
        ITextureManager textureManager)
    {
        var bindings = GetTextureBindings(materialName)
                       ?? ResolveTextureBindings(materialName, textureManager);

        if (bindings.TryGetBinding(slot, out var binding))
            return binding.TextureHandle;

        // Fallback to defaults
        return slot switch
        {
            TextureSlot.NormalMap => textureManager.DefaultNormalTexture,
            TextureSlot.DiffuseMap => textureManager.DefaultWhiteTexture,
            _ => textureManager.DefaultBlackTexture
        };
    }

    /// <summary>
    /// Invalidates cached texture bindings for a material (e.g., when textures change).
    /// </summary>
    public void InvalidateTextureBindings(string materialName)
    {
        lock (_lock)
        {
            _textureBindings.Remove(materialName);
        }
    }
}
