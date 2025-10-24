using Microsoft.Extensions.Logging;

namespace HEngine.Rendering.Logging;

public static class RenderLogEvents
{
    public static readonly EventId InitializeStart = new(100, nameof(InitializeStart));
    public static readonly EventId InitializeSuccess = new(101, nameof(InitializeSuccess));
    public static readonly EventId InitializeFailure = new(102, nameof(InitializeFailure));
    public static readonly EventId Dispose = new(199, nameof(Dispose));

    public static readonly EventId PollEvents = new(200, nameof(PollEvents));
    
    public static readonly EventId BeginRender = new(300, nameof(BeginRender));
    public static readonly EventId EndRender = new(301, nameof(EndRender));
    public static readonly EventId BeginFrame = new(310, nameof(BeginFrame));
    public static readonly EventId EndFrame = new(311, nameof(EndFrame));
    public static readonly EventId Clear = new(320, nameof(Clear));
    public static readonly EventId Present = new(321, nameof(Present));

    public static readonly EventId PipelineStart = new(400, nameof(PipelineStart));
    public static readonly EventId PipelineEnd = new(401, nameof(PipelineEnd));
    public static readonly EventId PipelineContextNullWarn = new(402, nameof(PipelineContextNullWarn));
    public static readonly EventId PipelineError = new(499, nameof(PipelineError));
}