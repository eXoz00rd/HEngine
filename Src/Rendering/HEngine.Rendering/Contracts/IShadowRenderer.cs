using System.Numerics;

namespace HEngine.Core.Rendering.Contracts;

/// <summary>
/// Renders shadow passes: depth-only geometry into shadow map cascades.
/// </summary>
public interface IShadowRenderer
{
    /// <summary>
    /// Begins a depth-only render pass targeting the specified cascade DSV slot.
    /// </summary>
    void BeginShadowPass(int cascadeIndex, Matrix4x4 lightVP, int resolution);

    /// <summary>
    /// Submits a depth-only mesh to the active shadow pass.
    /// </summary>
    void RenderDepthOnlyMesh(Matrix4x4 transform, ReadOnlySpan<float> vertices, ReadOnlySpan<uint> indices);

    /// <summary>
    /// Ends the current shadow pass and transitions the cascade slice to SRV state.
    /// </summary>
    void EndShadowPass();

    /// <summary>
    /// Binds all shadow map cascades and the shadow constant buffer for the main render pass.
    /// </summary>
    void BindShadowResources(ReadOnlySpan<Matrix4x4> lightVPs, ReadOnlySpan<float> cascadeSplits);
}

