using HEngine.Core.Contracts;
using HEngine.Core.Rendering.Data;

namespace HEngine.Rendering.Components;

public struct MeshAsset : IComponent
{
    public string AssetPath;
    public AssetLoadState LoadState;
    public Vertex3D[]? Vertices;
    public uint[]? Indices;
    public string? ErrorMessage;

    public MeshAsset(string path)
    {
        AssetPath = path ?? throw new ArgumentNullException(nameof(path));
        LoadState = AssetLoadState.NotLoaded;
        Vertices = null;
        Indices = null;
        ErrorMessage = null;
    }

    public readonly bool IsLoaded => LoadState == AssetLoadState.Loaded && Vertices != null && Indices != null;
    public readonly bool HasFailed => LoadState == AssetLoadState.Failed;
    public readonly bool IsLoading => LoadState == AssetLoadState.Loading;
}
