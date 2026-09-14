using HEngine.Runtime.Time;

namespace HEngine.Runtime.Tests.Time;

public class GameTimeTests
{
    [Fact(DisplayName = "A new GameTime starts with no elapsed frames")]
    public void New_GameTime_Starts_Zeroed()
    {
        var gameTime = new GameTime();

        Assert.Equal(0f, gameTime.DeltaTime);
        Assert.Equal(0, gameTime.FrameCount);
        Assert.Equal(0f, gameTime.FPS);
    }

    [Fact(DisplayName = "Update counts frames until the FPS window closes")]
    public void Update_Counts_Frames()
    {
        var gameTime = new GameTime();

        gameTime.Update();
        gameTime.Update();
        gameTime.Update();

        Assert.Equal(3, gameTime.FrameCount);
    }

    [Fact(DisplayName = "A frame longer than the stall threshold is clamped to the fallback step")]
    public void Update_Clamps_Stalled_Frames()
    {
        var gameTime = new GameTime();

        Thread.Sleep(150);
        gameTime.Update();

        Assert.Equal(0.016f, gameTime.DeltaTime);
    }

    [Fact(DisplayName = "Reset returns every counter to its initial value")]
    public void Reset_Clears_Counters()
    {
        var gameTime = new GameTime();
        gameTime.Update();
        gameTime.Update();

        gameTime.Reset();

        Assert.Equal(0f, gameTime.DeltaTime);
        Assert.Equal(0, gameTime.FrameCount);
        Assert.Equal(0f, gameTime.FPS);
        Assert.Equal(TimeSpan.Zero, gameTime.TotalGameTime);
    }
}
