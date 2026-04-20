using System.Globalization;
using HEngine.Rendering.PostProcessing;

namespace HEngine.Rendering.Tests.PostProcessing;

internal sealed class RecordingPostProcessContext : IPostProcessCommandContext
{
    public int SourceRenderTargetIndex { get; set; } = PingPongRenderTargets.RenderTargetA;
    public int DestinationRenderTargetIndex { get; set; } = PingPongRenderTargets.RenderTargetB;
    public int Width { get; set; } = 1920;
    public int Height { get; set; } = 1080;
    public int DrawCallCount { get; private set; }
    public List<string> Constants { get; } = new();

    public void DrawFullscreenTriangle() => DrawCallCount++;

    public void SetConstantFloat(string name, float value) =>
        Constants.Add($"{name}={value.ToString(CultureInfo.InvariantCulture)}");

    public void SetConstantInt(string name, int value) =>
        Constants.Add($"{name}={value}");

    public void SetConstantFloat4(string name, float x, float y, float z, float w) =>
        Constants.Add($"{name}=({x.ToString(CultureInfo.InvariantCulture)},{y.ToString(CultureInfo.InvariantCulture)},{z.ToString(CultureInfo.InvariantCulture)},{w.ToString(CultureInfo.InvariantCulture)})");
}

