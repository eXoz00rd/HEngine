using HEngine.Core.Components.Rendering;
using HEngine.Core.Components.Transform;
using HEngine.Core.Managers;
using HEngine.Core.Queries;
using HEngine.Core.Rendering.Contracts;
using HEngine.Core.Rendering.Data;
using HEngine.Rendering.Components;
using HEngine.Rendering.Systems.Contracts;

namespace HEngine.Rendering.Systems;

public class MeshRenderingSystem : IMeshRenderingSystem
{
    private bool _disposed;
    private QueryBuilder _queryBuilder = null!;
    private WorldManager _world = null!;

    public void Initialize(WorldManager worldManager)
    {
        _world = worldManager;
        _queryBuilder = new QueryBuilder(worldManager.ComponentManager, worldManager.EntityManager);
    }

    public void Render(IRenderContext context)
    {
        if (_disposed)
            return;

        var meshQuery = _queryBuilder.With<Transform, Mesh>();

        foreach (var (entity, transform, mesh) in meshQuery)
        {
            if (_world.HasComponent<Culled>(entity))
                continue;

            var transformMatrix = transform.GetWorldMatrix(_world);

            Vertex3D[] vertices;
            uint[] indices;
            switch (mesh.VertexArrayId)
            {
                case 1:
                    (vertices, indices) = MeshPrimitives.CreateCube();
                    break;
                case 2:
                    (vertices, indices) = MeshPrimitives.CreatePlane(1.0f, 1.0f);
                    break;
                default:
                    (vertices, indices) = MeshPrimitives.CreateCube();
                    break;
            }

            var flat = Flatten(vertices);
            context.Renderer.DrawMesh(transformMatrix, flat, indices);
        }
    }

    private static float[] Flatten(ReadOnlySpan<Vertex3D> vertices)
    {
        var result = new float[vertices.Length * 12];
        var o = 0;
        for (var i = 0; i < vertices.Length; i++)
        {
            var v = vertices[i];
            result[o++] = v.Position.X;
            result[o++] = v.Position.Y;
            result[o++] = v.Position.Z;
            result[o++] = v.Normal.X;
            result[o++] = v.Normal.Y;
            result[o++] = v.Normal.Z;
            result[o++] = v.TexCoord.X;
            result[o++] = v.TexCoord.Y;
            result[o++] = v.Color.X;
            result[o++] = v.Color.Y;
            result[o++] = v.Color.Z;
            result[o++] = v.Color.W;
        }
        return result;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _queryBuilder = null!;
        _disposed = true;
    }
}