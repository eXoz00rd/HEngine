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
}

