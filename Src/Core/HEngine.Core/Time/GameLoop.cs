using HEngine.Core.Contracts;
using HEngine.Core.Managers;
using HEngine.Core.Rendering.Contracts;
using Microsoft.Extensions.Logging;

namespace HEngine.Core.Time;

public class GameLoop : IGameLoop
{
    private readonly GameTime _gameTime;
    private readonly ILogger<GameLoop> _logger;
    private readonly IRenderManager _renderManager;
    private readonly IRenderPipeline _renderPipeline;
    private readonly SystemManager _systemManager;

    public GameLoop(GameTime gameTime, SystemManager systemManager, IRenderPipeline renderPipeline,
        IRenderManager renderManager, ILogger<GameLoop> logger)
    {
        _gameTime = gameTime ?? throw new ArgumentNullException(nameof(gameTime));
        _systemManager = systemManager ?? throw new ArgumentNullException(nameof(systemManager));
        _renderPipeline = renderPipeline ?? throw new ArgumentNullException(nameof(renderPipeline));
        _renderManager = renderManager ?? throw new ArgumentNullException(nameof(renderManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public bool IsRunning { get; private set; }

    public void Run()
    {
        if (IsRunning) return;

        IsRunning = true;
        _logger.LogInformation("Starting game loop");

        _gameTime.Reset();

        while (IsRunning && !_renderManager.ShouldClose)
            try
            {
                _gameTime.Update();
                _logger.LogTrace("Frame Time: {DeltaTime:F4}ms ({Fps:F2} FPS)", _gameTime.DeltaTime * 1000,
                    1 / _gameTime.DeltaTime);

                _renderManager.UpdateInput();
                _systemManager.Update(_gameTime.DeltaTime);

                _renderPipeline.RenderFrame();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in game loop. The loop will now stop.");
                IsRunning = false;
            }

        _logger.LogInformation("Game loop ended");
    }

    public void Stop()
    {
        IsRunning = false;
    }
}