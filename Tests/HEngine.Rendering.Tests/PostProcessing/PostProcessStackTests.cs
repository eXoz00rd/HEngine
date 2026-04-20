using HEngine.Rendering.PostProcessing;

namespace HEngine.Rendering.Tests.PostProcessing;

file sealed class FakeEffect : IPostProcessEffect
{
    private readonly List<int> _executionLog;

    public FakeEffect(string name, int order, List<int> executionLog)
    {
        Name = name;
        Order = order;
        _executionLog = executionLog;
        IsEnabled = true;
    }

    public string Name { get; }
    public bool IsEnabled { get; set; }
    public int Order { get; }
    public int ExecuteCount { get; private set; }

    public void Execute(IPostProcessCommandContext context)
    {
        ExecuteCount++;
        _executionLog.Add(Order);
    }
}

public class PostProcessStackTests
{
    [Fact(DisplayName = "PostProcessStack executes effects in ascending Order")]
    public void PostProcessStack_ExecutesEffects_InOrder()
    {
        var log = new List<int>();
        var stack = new PostProcessStack();
        stack.AddEffect(new FakeEffect("C", 300, log));
        stack.AddEffect(new FakeEffect("A", 100, log));
        stack.AddEffect(new FakeEffect("B", 200, log));

        var ctx = new RecordingPostProcessContext();
        stack.Execute(ctx);

        Assert.Equal([100, 200, 300], log);
    }

    [Fact(DisplayName = "PostProcessStack skips disabled effects")]
    public void PostProcessStack_Skips_DisabledEffects()
    {
        var log = new List<int>();
        var stack = new PostProcessStack();
        var a = new FakeEffect("A", 100, log) { IsEnabled = true };
        var b = new FakeEffect("B", 200, log) { IsEnabled = false };
        var c = new FakeEffect("C", 300, log) { IsEnabled = true };

        stack.AddEffect(a);
        stack.AddEffect(b);
        stack.AddEffect(c);

        var ctx = new RecordingPostProcessContext();
        stack.Execute(ctx);

        Assert.Equal([100, 300], log);
        Assert.Equal(0, b.ExecuteCount);
    }

    [Fact(DisplayName = "PostProcessStack EnabledEffectCount counts only enabled effects")]
    public void PostProcessStack_EnabledEffectCount_CountsOnlyEnabled()
    {
        var log = new List<int>();
        var stack = new PostProcessStack();
        stack.AddEffect(new FakeEffect("A", 100, log) { IsEnabled = true });
        stack.AddEffect(new FakeEffect("B", 200, log) { IsEnabled = false });
        stack.AddEffect(new FakeEffect("C", 300, log) { IsEnabled = true });

        Assert.Equal(2, stack.EnabledEffectCount);
    }

    [Fact(DisplayName = "PostProcessStack.Execute flips ping-pong after each enabled effect")]
    public void PostProcessStack_Execute_FlipsPingPong_PerEnabledEffect()
    {
        var log = new List<int>();
        var stack = new PostProcessStack();
        stack.AddEffect(new FakeEffect("A", 100, log));
        stack.AddEffect(new FakeEffect("B", 200, log));
        stack.AddEffect(new FakeEffect("C", 300, log));

        var ctx = new RecordingPostProcessContext();
        stack.Execute(ctx);

        Assert.Equal(3, stack.PingPong.FlipCount);
    }

    [Fact(DisplayName = "PostProcessStack.Execute resets ping-pong before each run")]
    public void PostProcessStack_Execute_ResetsPingPong_BeforeRun()
    {
        var log = new List<int>();
        var stack = new PostProcessStack();
        stack.AddEffect(new FakeEffect("A", 100, log));

        var ctx = new RecordingPostProcessContext();
        stack.Execute(ctx);
        stack.Execute(ctx);

        Assert.Equal(1, stack.PingPong.FlipCount);
    }

    [Fact(DisplayName = "PostProcessStack can remove effect by instance")]
    public void PostProcessStack_RemoveEffect_ByInstance()
    {
        var log = new List<int>();
        var stack = new PostProcessStack();
        var effect = new FakeEffect("A", 100, log);
        stack.AddEffect(effect);
        stack.AddEffect(new FakeEffect("B", 200, log));

        stack.RemoveEffect(effect);

        Assert.Single(stack.Effects);
    }

    [Fact(DisplayName = "PostProcessStack can remove effect by name")]
    public void PostProcessStack_RemoveEffect_ByName()
    {
        var log = new List<int>();
        var stack = new PostProcessStack();
        stack.AddEffect(new FakeEffect("MyEffect", 100, log));
        stack.AddEffect(new FakeEffect("Other", 200, log));

        stack.RemoveEffect("MyEffect");

        Assert.Single(stack.Effects);
        Assert.Equal("Other", stack.Effects[0].Name);
    }

    [Fact(DisplayName = "PostProcessStack.GetEffect<T> retrieves typed effect")]
    public void PostProcessStack_GetEffect_Returns_CorrectType()
    {
        var stack = new PostProcessStack();
        stack.AddEffect(new ToneMappingEffect());
        stack.AddEffect(new BloomEffect());

        var tone = stack.GetEffect<ToneMappingEffect>();
        var bloom = stack.GetEffect<BloomEffect>();
        var fxaa = stack.GetEffect<FxaaEffect>();

        Assert.NotNull(tone);
        Assert.NotNull(bloom);
        Assert.Null(fxaa);
    }

    [Fact(DisplayName = "PostProcessStack.Execute is a no-op when all effects are disabled")]
    public void PostProcessStack_Execute_IsNoOp_WhenAllDisabled()
    {
        var stack = new PostProcessStack();
        stack.AddEffect(new ToneMappingEffect { IsEnabled = false });
        stack.AddEffect(new BloomEffect { IsEnabled = false });

        var ctx = new RecordingPostProcessContext();
        stack.Execute(ctx);

        Assert.Equal(0, stack.PingPong.FlipCount);
    }
}


