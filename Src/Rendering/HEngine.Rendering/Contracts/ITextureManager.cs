namespace HEngine.Core.Rendering.Contracts;

/// <summary>
/// Platform-agnostic contract for managing GPU textures.
/// Implemented by rendering layer (e.g., DirectX 12).
/// </summary>
public interface ITextureManager : IDisposable
{
    /// <summary>
    /// Loads a texture from disk and creates a GPU resource. Returns a handle for binding.
    /// </summary>
    int LoadTexture(string filePath);

    /// <summary>
    /// Asynchronously loads a texture from disk and creates a GPU resource.
    /// </summary>
    Task<int> LoadTextureAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases a texture reference. GPU resource freed when ref count reaches zero.
    /// </summary>
    void ReleaseTexture(int textureHandle);

    /// <summary>
    /// Returns true if the texture handle is valid and loaded.
    /// </summary>
    bool IsTextureLoaded(int textureHandle);

    /// <summary>
    /// Gets the current reference count for a texture.
    /// </summary>
    int GetReferenceCount(int textureHandle);

    /// <summary>
    /// Number of currently loaded textures.
    /// </summary>
    int LoadedTextureCount { get; }

    /// <summary>
    /// Gets the handle for the default white (1x1) texture.
    /// </summary>
    int DefaultWhiteTexture { get; }

    /// <summary>
    /// Gets the handle for the default normal map (1x1, flat normal).
    /// </summary>
    int DefaultNormalTexture { get; }

    /// <summary>
    /// Gets the handle for the default black (1x1) texture.
    /// </summary>
    int DefaultBlackTexture { get; }
}

