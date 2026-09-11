using HEngine;
using HEngine.Runtime.Configuration;

var config = new EngineConfiguration();
config.Shadow.Enabled = true;

using var gameEngine = GameEngine.Create(config);
gameEngine.Run();