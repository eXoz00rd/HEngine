namespace HEngine.Rendering.PostProcessing;

/// <summary>
/// A single post-process effect that reads from a source render target and writes to a destination render target.
/// </summary>
public interface IPostProcessEffect
{
    string Name { get; }
    bool IsEnabled { get; set; }
    int Order { get; }
    void Execute(IPostProcessCommandContext context);
}

