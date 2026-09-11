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

    [Fact(DisplayName = "Update never reports a delta beyond the stall clamp")]
    public void Update_Clamps_Stalled_Frames()
    {
        var gameTime = new GameTime();

        gameTime.Update();

        Assert.True(gameTime.DeltaTime <= 0.1f);
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
