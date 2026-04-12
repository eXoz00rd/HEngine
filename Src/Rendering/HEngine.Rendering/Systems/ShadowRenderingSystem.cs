using System.Numerics;
using HEngine.Core.Components.Rendering;
using HEngine.Core.Components.Transform;
using HEngine.Core.Contracts;
using HEngine.Core.Mathematics;
using HEngine.Core.Managers;
using HEngine.Core.Queries;
using HEngine.Core.Rendering.Contracts;
using HEngine.Core.Rendering.Data;
using HEngine.Rendering.Components;

namespace HEngine.Rendering.Systems;

/// <summary>
/// Renders all shadow-casting entities into CSM cascade depth textures for a directional light.
/// </summary>
public class ShadowRenderingSystem : ISystem
{
    private bool _disposed;
    private WorldManager _world = null!;
    private QueryBuilder _queryBuilder = null!;
    private IShadowRenderer? _shadowRenderer;

    public void Initialize(WorldManager worldManager)
    {
        _world = worldManager;
        _queryBuilder = new QueryBuilder(worldManager.ComponentManager, worldManager.EntityManager);
    }

    public void SetShadowRenderer(IShadowRenderer? renderer)
    {
        _shadowRenderer = renderer;
    }

    /// <summary>
    /// Executes the full CSM shadow pass for a single directional light.
    /// </summary>
    public void RenderShadows(
        in Camera camera,
        Vector3 lightDirection,
        ReadOnlySpan<float> cascadeSplits,
        int resolution)
    {
        if (_disposed || _shadowRenderer is null) return;

        int cascadeCount = cascadeSplits.Length;
        var lightVPs = new Matrix4x4[cascadeCount];

        float prevSplit = camera.NearPlane;
        for (int i = 0; i < cascadeCount; i++)
        {
            float farSplit = cascadeSplits[i];
            var corners = ShadowUtils.GetFrustumCornersWorldSpace(camera, prevSplit, farSplit);
            // Renamed variable from lightVP to lightVp to match naming conventions (SonarQube fix)
            var lightVp = ShadowUtils.ComputeDirectionalLightVP(lightDirection, corners); 
            lightVPs[i] = ShadowUtils.SnapToTexelGrid(lightVp, resolution);
            prevSplit = farSplit;
        }

        for (int cascade = 0; cascade < cascadeCount; cascade++)
        {
            _shadowRenderer.BeginShadowPass(cascade, lightVPs[cascade], resolution);
            RenderShadowCasters();
            _shadowRenderer.EndShadowPass();
        }

        _shadowRenderer.BindShadowResources(lightVPs, cascadeSplits);
    }

    private void RenderShadowCasters()
    {
        var query = _queryBuilder.With<Transform, Mesh, Renderable>();
        foreach (var (entity, transform, mesh, renderable) in query)
        {
            if (!renderable.CastShadows) continue;
            if (_world.HasComponent<Culled>(entity)) continue;

            var worldMatrix = transform.GetWorldMatrix(_world);

            Vertex3D[] vertices;
            uint[] indices;
            switch (mesh.VertexArrayId)
            {
                case 1:
                    (vertices, indices) = MeshPrimitives.CreateCube(1.0f, mesh.Color);
                    break;
                case 2:
                    (vertices, indices) = MeshPrimitives.CreatePlane(1.0f, 1.0f, mesh.Color);
                    break;
                default:
                    (vertices, indices) = MeshPrimitives.CreateCube(1.0f, mesh.Color);
                    break;
            }

            _shadowRenderer!.RenderDepthOnlyMesh(worldMatrix, FlattenPositions(vertices), indices);
        }
    }

    private static float[] FlattenPositions(ReadOnlySpan<Vertex3D> vertices)
    {
        var result = new float[vertices.Length * 12];
        var o = 0;
        for (int i = 0; i < vertices.Length; i++)
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

    public void Update(float deltaTime) { }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Cleanup managed resources: dispose IDisposable objects and nullify references.
                if (_shadowRenderer != null) 
                {
                    (_shadowRenderer as IDisposable)?.Dispose();
                    _shadowRenderer = null;
                }

                _queryBuilder = null!; // Nullify reference for GC
            }

            // Cleanup unmanaged resources: nullify non-managed references.
            _world = null!;
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}