using System.Numerics;
using HEngine.Core.Rendering.Contracts;
using HEngine.Core.Rendering.Data;

namespace HEngine.Rendering.Renderers;

public sealed class DirectX12MeshRenderer : IDisposable
{
    public bool IsInitialized { get; private set; }
    public bool DepthTestEnabled { get; private set; } = true;
    public bool BackFaceCullingEnabled { get; private set; } = true;

    public Matrix4x4 LastMvp { get; private set; } = Matrix4x4.Identity;
    public int LastDrawVertexCount { get; private set; }
    public int LastDrawIndexCount { get; private set; }

    public void Initialize(object? device = null)
    {
        IsInitialized = true;
    }

    public void SetDepthTest(bool enabled) => DepthTestEnabled = enabled;
    public void SetBackFaceCulling(bool enabled) => BackFaceCullingEnabled = enabled;
    
    public void DrawMesh(Matrix4x4 modelMatrix,
                         ReadOnlySpan<Vertex3D> vertices,
                         ReadOnlySpan<uint> indices,
                         IRenderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        LastMvp = modelMatrix * context.ViewMatrix * context.ProjectionMatrix;

        LastDrawVertexCount = vertices.Length;
        LastDrawIndexCount = indices.Length;
    }

    public void Dispose()
    {
        IsInitialized = false;
    }
}
