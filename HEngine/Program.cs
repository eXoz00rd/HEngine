using HEngine;
using HEngine.Core.Configuration;

var config = new EngineConfiguration();
config.Shadow.Enabled = true;

using var gameEngine = GameEngine.Create(config);
gameEngine.Run();