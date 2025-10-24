using Timer = HEngine.Core.Components.Core.Timer;

namespace HEngine.Core.Tests.Components.Core;

public class TimerTests {
    [Fact]
    public void Constructor_Default_ShouldSetCorrectDefaults()
    {
        
        const float duration = 5.0f;

        
        var timer = new Timer(duration);

        
        Assert.Equal(duration, timer.Duration);
        Assert.Equal(0f, timer.Elapsed);
        Assert.False(timer.IsRepeating);
        Assert.True(timer.IsActive);
        Assert.False(timer.AutoDestroy);
    }

    [Theory]
    [InlineData(5.0f, true, true)]
    [InlineData(2.0f, false, false)]
    public void Constructor_WithParameters_ShouldSetCorrectValues(float duration, bool repeating, bool autoDestroy)
    {
        
        var timer = new Timer(duration, repeating, autoDestroy);

        
        Assert.Equal(duration, timer.Duration);
        Assert.Equal(repeating, timer.IsRepeating);
        Assert.Equal(autoDestroy, timer.AutoDestroy);
    }

    [Fact]
    public void CreateLifetime_ShouldCreateCorrectTimer()
    {
        
        const float duration = 3.0f;

        
        var timer = Timer.CreateLifetime(duration);

        
        Assert.Equal(duration, timer.Duration);
        Assert.False(timer.IsRepeating);
        Assert.True(timer.AutoDestroy);
        Assert.True(timer.IsActive);
    }

    [Theory]
    [InlineData(5.0f, 0.0f, 0.0f)]
    [InlineData(5.0f, 2.5f, 0.5f)]
    [InlineData(5.0f, 5.0f, 1.0f)]
    [InlineData(5.0f, 7.5f, 1.0f)]
    public void Progress_ShouldReturnCorrectValue(float duration, float elapsed, float expected)
    {
        
        var timer = new Timer(duration);
        timer.Elapsed = elapsed;

        
        var result = timer.Progress;

        
        Assert.Equal(expected, result, 3);
    }

    [Fact]
    public void Progress_WithZeroDuration_ShouldReturnOne()
    {
        
        var timer = new Timer(0f);

        
        var result = timer.Progress;

        
        Assert.Equal(1.0f, result);
    }

    [Theory]
    [InlineData(5.0f, 4.9f, false)]
    [InlineData(5.0f, 5.0f, true)]
    [InlineData(5.0f, 5.1f, true)]
    public void IsCompleted_ShouldReturnCorrectValue(float duration, float elapsed, bool expected)
    {
        
        var timer = new Timer(duration);
        timer.Elapsed = elapsed;

        
        var result = timer.IsCompleted;

        
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, true)]
    public void ShouldDestroy_ShouldReturnCorrectValue(bool autoDestroy, bool isCompleted, bool expected)
    {
        
        var timer = new Timer(5.0f, autoDestroy: autoDestroy);
        if (isCompleted)
            timer.Elapsed = 5.0f;

        
        var result = timer.ShouldDestroy;

        
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Update_WhenActive_ShouldUpdateElapsed()
    {
        
        var timer = new Timer(5.0f);
        const float deltaTime = 1.0f;

        
        timer.Update(deltaTime);

        
        Assert.Equal(deltaTime, timer.Elapsed);
    }

    [Fact]
    public void Update_WhenInactive_ShouldNotUpdateElapsed()
    {
        
        var timer = new Timer(5.0f);
        timer.IsActive = false;
        const float deltaTime = 1.0f;

        
        timer.Update(deltaTime);

        
        Assert.Equal(0f, timer.Elapsed);
    }

    [Fact]
    public void Update_RepeatingTimer_ShouldResetWhenCompleted()
    {
        
        var timer = new Timer(5.0f, true);
        timer.Elapsed = 4.0f;
        const float deltaTime = 2.0f;

        
        timer.Update(deltaTime);

        
        Assert.Equal(0.0f, timer.Elapsed);
    }

    [Fact]
    public void Update_RepeatingTimer_ShouldResetToZeroWhenCompleted()
    {
        
        var timer = new Timer(5.0f, true);
        timer.Elapsed = 4.0f;
        const float deltaTime = 1.5f;

        
        timer.Update(deltaTime);

        
        Assert.Equal(0.0f, timer.Elapsed);
    }

    [Fact]
    public void Update_NonRepeatingTimer_ShouldNotResetWhenCompleted()
    {
        
        var timer = new Timer(5.0f);
        timer.Elapsed = 4.0f;
        const float deltaTime = 2.0f;

        
        timer.Update(deltaTime);

        
        Assert.Equal(6.0f, timer.Elapsed);
    }
}