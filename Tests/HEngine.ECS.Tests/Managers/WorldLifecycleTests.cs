using HEngine.Core.Managers;

namespace HEngine.ECS.Tests.Managers;

public class WorldLifecycleTests : IDisposable {
    private readonly WorldManager _worldManager = new(new SystemManager());

    public void Dispose()
        => _worldManager.Dispose();

    [Fact]
    public void LifecycleState_Initially_ShouldBeEmpty()
    {
        Assert.Equal(WorldLifecycleState.Empty, _worldManager.LifecycleState);
    }

    [Fact]
    public void Load_FromEmpty_ShouldTransitionToEdit()
    {
        _worldManager.Load();

        Assert.Equal(WorldLifecycleState.Edit, _worldManager.LifecycleState);
    }

    [Fact]
    public void Load_WhenNotEmpty_ShouldThrowInvalidOperationException()
    {
        _worldManager.Load();

        Assert.Throws<InvalidOperationException>(() => _worldManager.Load());
    }

    [Fact]
    public void EnterPlay_FromEdit_ShouldTransitionToPlay()
    {
        _worldManager.Load();

        _worldManager.EnterPlay();

        Assert.Equal(WorldLifecycleState.Play, _worldManager.LifecycleState);
    }

    [Fact]
    public void EnterPlay_WhenNotEdit_ShouldThrowInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => _worldManager.EnterPlay());
    }

    [Fact]
    public void Pause_FromPlay_ShouldTransitionToPause()
    {
        _worldManager.Load();
        _worldManager.EnterPlay();

        _worldManager.Pause();

        Assert.Equal(WorldLifecycleState.Pause, _worldManager.LifecycleState);
    }

    [Fact]
    public void Pause_WhenNotPlay_ShouldThrowInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => _worldManager.Pause());
    }

    [Fact]
    public void Resume_FromPause_ShouldTransitionToPlay()
    {
        _worldManager.Load();
        _worldManager.EnterPlay();
        _worldManager.Pause();

        _worldManager.Resume();

        Assert.Equal(WorldLifecycleState.Play, _worldManager.LifecycleState);
    }

    [Fact]
    public void Resume_WhenNotPause_ShouldThrowInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => _worldManager.Resume());
    }

    [Fact]
    public void Resume_WithUnconsumedPendingStep_ShouldClearPendingStep()
    {
        _worldManager.Load();
        _worldManager.EnterPlay();
        _worldManager.Pause();
        _worldManager.Step();

        _worldManager.Resume();

        Assert.False(_worldManager.HasPendingStep);

        _worldManager.Pause();
        Assert.False(_worldManager.HasPendingStep);
    }

    [Fact]
    public void Step_WhilePaused_ShouldStayPausedAndSetPendingStep()
    {
        _worldManager.Load();
        _worldManager.EnterPlay();
        _worldManager.Pause();

        _worldManager.Step();

        Assert.Equal(WorldLifecycleState.Pause, _worldManager.LifecycleState);
        Assert.True(_worldManager.HasPendingStep);
    }

    [Fact]
    public void Step_WhenNotPaused_ShouldThrowInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => _worldManager.Step());
    }

    [Fact]
    public void ConsumePendingStep_WithPendingStep_ShouldReturnTrueAndClearFlag()
    {
        _worldManager.Load();
        _worldManager.EnterPlay();
        _worldManager.Pause();
        _worldManager.Step();

        var consumed = _worldManager.ConsumePendingStep();

        Assert.True(consumed);
        Assert.False(_worldManager.HasPendingStep);
    }

    [Fact]
    public void ConsumePendingStep_WithoutPendingStep_ShouldReturnFalse()
    {
        var consumed = _worldManager.ConsumePendingStep();

        Assert.False(consumed);
    }

    [Fact]
    public void ExitPlay_FromPlay_ShouldTransitionToEdit()
    {
        _worldManager.Load();
        _worldManager.EnterPlay();

        _worldManager.ExitPlay();

        Assert.Equal(WorldLifecycleState.Edit, _worldManager.LifecycleState);
    }

    [Fact]
    public void ExitPlay_FromPause_ShouldTransitionToEditAndClearPendingStep()
    {
        _worldManager.Load();
        _worldManager.EnterPlay();
        _worldManager.Pause();
        _worldManager.Step();

        _worldManager.ExitPlay();

        Assert.Equal(WorldLifecycleState.Edit, _worldManager.LifecycleState);
        Assert.False(_worldManager.HasPendingStep);
    }

    [Theory]
    [InlineData(WorldLifecycleState.Empty)]
    [InlineData(WorldLifecycleState.Edit)]
    public void ExitPlay_WhenNotPlayOrPause_ShouldThrowInvalidOperationException(WorldLifecycleState state)
    {
        if (state == WorldLifecycleState.Edit)
            _worldManager.Load();

        Assert.Throws<InvalidOperationException>(() => _worldManager.ExitPlay());
    }

    [Fact]
    public void FullLifecycle_ShouldFollowDiagramTransitions()
    {
        Assert.Equal(WorldLifecycleState.Empty, _worldManager.LifecycleState);

        _worldManager.Load();
        Assert.Equal(WorldLifecycleState.Edit, _worldManager.LifecycleState);

        _worldManager.EnterPlay();
        Assert.Equal(WorldLifecycleState.Play, _worldManager.LifecycleState);

        _worldManager.Pause();
        Assert.Equal(WorldLifecycleState.Pause, _worldManager.LifecycleState);

        _worldManager.Step();
        Assert.True(_worldManager.HasPendingStep);
        Assert.Equal(WorldLifecycleState.Pause, _worldManager.LifecycleState);

        _worldManager.Resume();
        Assert.Equal(WorldLifecycleState.Play, _worldManager.LifecycleState);

        _worldManager.Pause();
        _worldManager.ExitPlay();
        Assert.Equal(WorldLifecycleState.Edit, _worldManager.LifecycleState);
    }

    [Fact]
    public void LifecycleTransitions_OnDisposedWorldManager_ShouldThrowObjectDisposedException()
    {
        _worldManager.Dispose();

        Assert.Throws<ObjectDisposedException>(() => _worldManager.Load());
        Assert.Throws<ObjectDisposedException>(() => _worldManager.ConsumePendingStep());
    }
}
