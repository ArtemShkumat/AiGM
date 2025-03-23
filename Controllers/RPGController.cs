using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AiGMBackEnd.Models;
using AiGMBackEnd.Services;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace AiGMBackEnd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RPGController : ControllerBase
    {
        private readonly PresenterService _presenterService;
        private readonly LoggingService _loggingService;
        private readonly StorageService _storageService;

        public RPGController(PresenterService presenterService, LoggingService loggingService, StorageService storageService)
        {
            _presenterService = presenterService;
            _loggingService = loggingService;
            _storageService = storageService;
        }

        [HttpPost("input")]
        public async Task<IActionResult> ProcessUserInput([FromBody] UserInputRequest request)
        {
            if (string.IsNullOrEmpty(request.GameId))
            {
                return BadRequest("GameId is required");
            }

            if (string.IsNullOrEmpty(request.UserInput))
            {
                return BadRequest("UserInput is required");
            }

            try
            {
                var response = await _presenterService.HandleUserInputAsync(request.GameId, request.UserInput, request.PromptType);
                
                return Ok(new UserInputResponse
                {
                    Response = response,
                    Success = true
                });
            }
            catch (System.Exception ex)
            {
                _loggingService.LogError($"Error processing user input: {ex.Message}");
                
                return StatusCode(500, new UserInputResponse
                {
                    Response = "An error occurred while processing your request.",
                    Success = false,
                    Error = ex.Message
                });
            }
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
                    
                    if (world != null)
                    {
                        if (!string.IsNullOrEmpty(world.GameName))
                        {
                            gameName = world.GameName;
                        }
                        if (!string.IsNullOrEmpty(world.Setting))
                        {
                            gameDescription = world.Setting;
                        }
                    }
                    else if (gameSetting != null)
                    {
                        if (!string.IsNullOrEmpty(gameSetting.Genre))
                        {
                            gameName = gameSetting.Genre;
                        }
                        if (!string.IsNullOrEmpty(gameSetting.Description))
                        {
                            gameDescription = gameSetting.Description;
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

        [HttpPost("createCharacter")]
        public async Task<IActionResult> CreateCharacter([FromBody] CreateCharacterRequest request)
        {
            if (string.IsNullOrEmpty(request.GameId))
            {
                return BadRequest("GameId is required");
            }
            
            if (string.IsNullOrEmpty(request.CharacterDescription))
            {
                return BadRequest("CharacterDescription is required");
            }
            
            try
            {
                // Create a job using the CreatePlayerJson prompt type
                var result = await _presenterService.HandleUserInputAsync(
                    request.GameId, 
                    request.CharacterDescription, 
                    PromptType.CreatePlayerJson);
                
                return Ok(new { 
                    Success = true,
                    Message = "Character created successfully", 
                    Details = result 
                });
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error creating character: {ex.Message}");
                return StatusCode(500, new { 
                    Success = false, 
                    Error = $"Error creating character: {ex.Message}" 
                });
            }
        }

        [HttpGet("scene")]
        public async Task<IActionResult> Scene(string gameId)
        {
            if (string.IsNullOrEmpty(gameId))
            {
                return BadRequest("GameId is required");
            }
            
            try
            {
                // Load player data to get current location
                var player = await _storageService.GetPlayerAsync(gameId);
                
                if (player == null)
                {
                    return BadRequest("Player data not found. Character may not have been created yet.");
                }
                
                var currentLocationId = player.CurrentLocationId;
                
                // Get visible NPCs in the location
                var visibleNpcs = await _storageService.GetVisibleNpcsInLocationAsync(gameId, currentLocationId);
                
                // Get location data
                var location = await _storageService.GetLocationAsync(gameId, currentLocationId);
                
                var sceneElements = new SceneInfo
                {
                    GameId = gameId,
                    CurrentLocationId = currentLocationId,
                    VisibleNpcs = visibleNpcs
                };
                
                if (location != null)
                {
                    sceneElements.LocationName = location.Name;
                    sceneElements.LocationDescription = location.Description;
                }
                
                return Ok(sceneElements);
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error getting scene for game {gameId}: {ex.Message}");
                return StatusCode(500, $"Error retrieving scene: {ex.Message}");
            }
        }

        [HttpGet("visibleNpcs")]
        public async Task<IActionResult> GetVisibleNpcs(string gameId)
        {
            if (string.IsNullOrEmpty(gameId))
            {
                return BadRequest("GameId is required");
            }
            
            try
            {
                var visibleNpcs = await _storageService.GetAllVisibleNpcsAsync(gameId);
                return Ok(visibleNpcs);
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error getting visible NPCs for game {gameId}: {ex.Message}");
                return StatusCode(500, $"Error retrieving visible NPCs: {ex.Message}");
            }
        }

        [HttpGet("player")]
        public async Task<IActionResult> GetPlayerInfo(string gameId)
        {
            if (string.IsNullOrEmpty(gameId))
            {
                return BadRequest("GameId is required");
            }
            
            try
            {
                var player = await _storageService.GetPlayerAsync(gameId);
                
                if (player == null)
                {
                    return BadRequest("Player data not found. Character may not have been created yet.");
                }
                
                // Create a new anonymous object without the inventory
                var playerInfo = new
                {
                    player.Name,
                    player.VisualDescription,
                    player.Age,
                    player.Backstory,
                    player.CurrentLocationId,
                    player.Money,
                    player.StatusEffects,
                    player.RpgElements,
                    player.ActiveQuests
                };
                
                return Ok(playerInfo);
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error getting player info for game {gameId}: {ex.Message}");
                return StatusCode(500, $"Error retrieving player info: {ex.Message}");
            }
        }

        [HttpGet("inventory")]
        public async Task<IActionResult> GetPlayerInventory(string gameId)
        {
            if (string.IsNullOrEmpty(gameId))
            {
                return BadRequest("GameId is required");
            }
            
            try
            {
                var player = await _storageService.GetPlayerAsync(gameId);
                
                if (player == null)
                {
                    return BadRequest("Player data not found. Character may not have been created yet.");
                }
                
                return Ok(player.Inventory);
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error getting inventory for game {gameId}: {ex.Message}");
                return StatusCode(500, $"Error retrieving inventory: {ex.Message}");
            }
        }
    }

    public class NewGameRequest
    {
        public string ScenarioId { get; set; }
        public GamePreferences Preferences { get; set; }
    }

    public class ScenarioInfo
    {
        public string ScenarioId { get; set; }
        public string Name { get; set; }
        public GameSetting GameSetting { get; set; }
        public GamePreferences GamePreferences { get; set; }
    }

    public class UserInputRequest
    {
        public string GameId { get; set; }
        public string UserInput { get; set; }
        public PromptType PromptType { get; set; } = PromptType.DM; // Default to DM prompt
    }

    public class UserInputResponse
    {
        public string Response { get; set; }
        public bool Success { get; set; }
        public string Error { get; set; }
    }

    public class GameInfo
    {
        public string GameId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class CreateCharacterRequest
    {
        public string GameId { get; set; }
        public string CharacterDescription { get; set; }
    }

    public class SceneInfo
    {
        public string GameId { get; set; }
        public string CurrentLocationId { get; set; }
        public string LocationName { get; set; }
        public string LocationDescription { get; set; }
        public List<Services.NpcInfo> VisibleNpcs { get; set; } = new List<Services.NpcInfo>();
    }
}
