using HEngine.Core.Contracts;

namespace HEngine.Rendering.Components;

public struct Mesh : IComponent {
    public uint VertexArrayId;
    public int IndexCount;
    public string MaterialPath;

    public Mesh(uint vertexArrayId, int indexCount, string materialPath = "")
    {
        VertexArrayId = vertexArrayId;
        IndexCount = indexCount;
        MaterialPath = materialPath;
    }
}