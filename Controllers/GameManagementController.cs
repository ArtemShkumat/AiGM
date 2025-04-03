using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AiGMBackEnd.Models;
using AiGMBackEnd.Services;
using Microsoft.AspNetCore.Mvc;

namespace AiGMBackEnd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GameManagementController : ControllerBase
    {
        private readonly PresenterService _presenterService;
        private readonly LoggingService _loggingService;
        private readonly StorageService _storageService;

        public GameManagementController(PresenterService presenterService, LoggingService loggingService, StorageService storageService)
        {
            _presenterService = presenterService;
            _loggingService = loggingService;
            _storageService = storageService;
        }

        [HttpGet("scenarios")]
        public async Task<IActionResult> GetScenarios()
        {
            try
            {
                var scenarioIds = _storageService.GetScenarioIds();
                var scenarios = new List<ScenarioInfo>();
                
                foreach (var scenarioId in scenarioIds)
                {
                    var gameSetting = await _storageService.LoadScenarioSettingAsync<GameSetting>(scenarioId, "gameSetting.json");
                    var gamePreferences = await _storageService.LoadScenarioSettingAsync<GamePreferences>(scenarioId, "gamePreferences.json");
                    
                    var scenarioInfo = new ScenarioInfo
                    {
                        ScenarioId = scenarioId,
                        Name = scenarioId.Replace("_", " "),
                        GameSetting = gameSetting,
                        GamePreferences = gamePreferences
                    };
                    
                    scenarios.Add(scenarioInfo);
                }
                
                return Ok(scenarios);
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error retrieving scenarios: {ex.Message}");
                return StatusCode(500, "Error retrieving available scenarios.");
            }
        }

        [HttpPost("createGame")]
        public async Task<IActionResult> CreateNewGame([FromBody] NewGameRequest req)
        {
            try
            {
                if (string.IsNullOrEmpty(req.ScenarioId))
                {
                    return BadRequest("ScenarioId is required");
                }

                var gameId = await _storageService.CreateGameFromScenarioAsync(req.ScenarioId, req.Preferences);
                
                return Ok(new { gameId });
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error creating new game: {ex.Message}");
                return StatusCode(500, $"Error creating new game: {ex.Message}");
            }
        }

        [HttpGet("listGames")]
        public async Task<IActionResult> ListGames()
        {
            try
            {
                var gameIds = _storageService.GetGameIds();
                var games = new List<GameInfo>();
                
                foreach (var gameId in gameIds)
                {
                    var world = await _storageService.GetWorldAsync(gameId);
                    var gameSetting = await _storageService.GetGameSettingAsync(gameId);
                    
                    var gameName = gameId;
                    var gameDescription = "No description available";
                                        
                    if (gameSetting != null)
                    {
                        if (!string.IsNullOrEmpty(gameSetting.Genre))
                        {
                            gameName = gameSetting.Genre;
                        }
                        if (!string.IsNullOrEmpty(gameSetting.Description))
                        {
                            gameDescription = gameSetting.Description;
                        }
                        if (!string.IsNullOrEmpty(gameSetting.GameName))
                        {
                            gameName = gameSetting.GameName;
                        }
                        if (!string.IsNullOrEmpty(gameSetting.Setting))
                        {
                            gameDescription = gameSetting.Setting;
                        }
                    }
                    
                    games.Add(new GameInfo
                    {
                        GameId = gameId,
                        Name = gameName,
                        Description = gameDescription
                    });
                }
                
                return Ok(games);
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error listing games: {ex.Message}");
                return StatusCode(500, "Error retrieving available games.");
            }
        }
    }
} 