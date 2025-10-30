using HEngine.Core.Contracts;

namespace HEngine.Rendering.Components;

public struct Mesh : IComponent {
    public uint VertexArrayId;
    public int IndexCount;
    public string MaterialPath;
    public System.Numerics.Vector4 Color;

    public Mesh(uint vertexArrayId, int indexCount, string materialPath = "")
    {
        VertexArrayId = vertexArrayId;
        IndexCount = indexCount;
        MaterialPath = materialPath;
        Color = new System.Numerics.Vector4(1, 1, 1, 1);
    }
}