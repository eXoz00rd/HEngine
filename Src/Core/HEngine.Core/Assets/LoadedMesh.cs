using HEngine.Core.Rendering.Data;

namespace HEngine.Core.Assets;

public class LoadedMesh
{
    public Vertex3D[] Vertices { get; }
    public uint[] Indices { get; }

    public LoadedMesh(Vertex3D[] vertices, uint[] indices)
    {
        Vertices = vertices ?? throw new ArgumentNullException(nameof(vertices));
        Indices = indices ?? throw new ArgumentNullException(nameof(indices));
    }
}
