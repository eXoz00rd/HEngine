using HEngine.Rendering.Enums;

namespace HEngine.Rendering.Data;

/// <summary>
/// Maps texture slot names to texture handles and shader slots.
/// Used by materials to track which textures are bound.
/// </summary>
public sealed class MaterialTextureBindings
{
    private readonly Dictionary<TextureSlot, TextureBinding> _bindings = new();

    /// <summary>
    /// Binds a texture to a specific slot.
    /// </summary>
    public void Bind(TextureSlot slot, int textureHandle, string sourcePath = "")
    {
        _bindings[slot] = new TextureBinding(textureHandle, sourcePath);
    }

    /// <summary>
    /// Unbinds a texture from a slot.
    /// </summary>
    public void Unbind(TextureSlot slot)
    {
        _bindings.Remove(slot);
    }

    /// <summary>
    /// Gets the binding for a slot, if any.
    /// </summary>
    public bool TryGetBinding(TextureSlot slot, out TextureBinding binding)
    {
        return _bindings.TryGetValue(slot, out binding);
    }

    /// <summary>
    /// Checks if a slot has a texture bound.
    /// </summary>
    public bool HasBinding(TextureSlot slot)
    {
        return _bindings.ContainsKey(slot);
    }

    /// <summary>
    /// Returns all current bindings.
    /// </summary>
    public IReadOnlyDictionary<TextureSlot, TextureBinding> GetAll() => _bindings;

    /// <summary>
    /// Number of bound texture slots.
    /// </summary>
    public int BoundCount => _bindings.Count;

    /// <summary>
    /// Clears all bindings.
    /// </summary>
    public void Clear() => _bindings.Clear();

    /// <summary>
    /// Resolves material property block texture paths to texture handles using the provided resolver function.
    /// Maps standard property names to standard slots:
    ///   _DiffuseTexture → DiffuseMap (t0)
    ///   _NormalTexture  → NormalMap (t1)
    ///   _MetallicRoughnessTexture → MetallicRoughnessMap (t2)
    ///   _EmissiveTexture → EmissiveMap (t3)
    ///   _AOTexture → AOMap (t4)
    /// </summary>
    public void ResolveFromPropertyBlock(MaterialPropertyBlock propertyBlock, Func<string, int> textureResolver)
    {
        ArgumentNullException.ThrowIfNull(propertyBlock);
        ArgumentNullException.ThrowIfNull(textureResolver);

        TryResolve(propertyBlock, "_DiffuseTexture", TextureSlot.DiffuseMap, textureResolver);
        TryResolve(propertyBlock, "_NormalTexture", TextureSlot.NormalMap, textureResolver);
        TryResolve(propertyBlock, "_MetallicRoughnessTexture", TextureSlot.MetallicRoughnessMap, textureResolver);
        TryResolve(propertyBlock, "_EmissiveTexture", TextureSlot.EmissiveMap, textureResolver);
        TryResolve(propertyBlock, "_AOTexture", TextureSlot.AOMap, textureResolver);
    }

    private void TryResolve(MaterialPropertyBlock block, string propertyName, TextureSlot slot, Func<string, int> resolver)
    {
        var path = block.GetTexture(propertyName);
        if (!string.IsNullOrEmpty(path))
        {
            var handle = resolver(path);
            Bind(slot, handle, path);
        }
    }
}

/// <summary>
/// Represents a single texture binding: a handle to a GPU texture and its source path.
/// </summary>
public readonly record struct TextureBinding(int TextureHandle, string SourcePath);

