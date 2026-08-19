namespace HEngine.Rendering.PostProcessing;

/// <summary>
/// Abstraction over GPU command list for post-process pass execution.
/// Enables headless unit testing without GPU dependency.
/// </summary>
public interface IPostProcessCommandContext
{
    int SourceRenderTargetIndex { get; }
    int DestinationRenderTargetIndex { get; }
    int Width { get; }
    int Height { get; }

    void DrawFullscreenTriangle();
    void SetConstantFloat(string name, float value);
    void SetConstantInt(string name, int value);
    void SetConstantFloat4(string name, float x, float y, float z, float w);

    /// <summary>
    /// Prepares the scene's HDR render target as the input for the first post-process pass this frame.
    /// Must be called once, before <see cref="PostProcessStack.Execute"/>.
    /// </summary>
    void PrepareSceneSource();

    /// <summary>
    /// Resolves the post-process chain's final output into the swap-chain back buffer. Must be called
    /// once, after <see cref="PostProcessStack.Execute"/> returns.
    /// </summary>
    void ResolveToBackBuffer();
}

