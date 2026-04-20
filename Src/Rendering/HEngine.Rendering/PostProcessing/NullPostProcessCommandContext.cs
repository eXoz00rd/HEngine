using HEngine.Core.Rendering.Contracts;

namespace HEngine.Rendering.PostProcessing;

/// <summary>
/// A no-op post-process command context used when no real GPU command list is available
/// (e.g. headless pipeline, unit tests). Records call counts for verification purposes.
/// </summary>
public sealed class NullPostProcessCommandContext : IPostProcessCommandContext
{
    private readonly IRenderContext _renderContext;

    public NullPostProcessCommandContext(IRenderContext renderContext)
    {
        ArgumentNullException.ThrowIfNull(renderContext);
        _renderContext = renderContext;
    }

    public int SourceRenderTargetIndex { get; private set; } = PingPongRenderTargets.RenderTargetA;
    public int DestinationRenderTargetIndex { get; private set; } = PingPongRenderTargets.RenderTargetB;
    public int Width { get; set; } = 1280;
    public int Height { get; set; } = 720;
    public int DrawCallCount { get; private set; }

    public void DrawFullscreenTriangle()
    {
        DrawCallCount++;
        (SourceRenderTargetIndex, DestinationRenderTargetIndex) =
            (DestinationRenderTargetIndex, SourceRenderTargetIndex);
    }

    public void SetConstantFloat(string name, float value) { }
    public void SetConstantInt(string name, int value) { }
    public void SetConstantFloat4(string name, float x, float y, float z, float w) { }
}

