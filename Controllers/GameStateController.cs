using System;
using System.Threading.Tasks;
using AiGMBackEnd.Models;
using AiGMBackEnd.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Linq;
using static AiGMBackEnd.Services.StorageService;

namespace AiGMBackEnd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GameStateController : ControllerBase
    {
        private readonly LoggingService _loggingService;
        private readonly StorageService _storageService;
        private readonly GameNotificationService _notificationService;

        public GameStateController(LoggingService loggingService, StorageService storageService, GameNotificationService notificationService)
        {
            _loggingService = loggingService;
            _storageService = storageService;
            _notificationService = notificationService;
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
                var visibleNpcs = await _storageService.GetNpcsInLocationAsync(gameId, currentLocationId);
                
                // Get location data
                var location = await _storageService.GetLocationAsync(gameId, currentLocationId);
                
                var sceneElements = new SceneInfo
                {
                    GameId = gameId,
                    CurrentLocationId = currentLocationId,
                    VisibleNpcs = visibleNpcs.Select(npc => new NpcInfo
                    {
                        Id = npc.Id,
                        Name = npc.Name
                    }).ToList()
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
                    player.Currencies,
                    player.StatusEffects,
                    player.RpgTags,
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

        [HttpGet("currencies")]
        public async Task<IActionResult> GetPlayerCurrencies(string gameId)
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
                
                return Ok(player.Currencies);
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error getting currencies for game {gameId}: {ex.Message}");
                return StatusCode(500, $"Error retrieving currencies: {ex.Message}");
            }
        }

        [HttpGet("allQuests")]
        public async Task<IActionResult> GetAllQuests(string gameId)
        {
            if (string.IsNullOrEmpty(gameId))
            {
                return BadRequest("GameId is required");
            }
            
            try
            {
                var allQuests = await _storageService.GetAllQuestsAsync(gameId);
                var simplifiedQuests = allQuests.Select(q => new
                {
                    Id = q.Id,
                    Title = q.Title,
                    Objective = q.CoreObjective
                }).ToList();
                
                return Ok(simplifiedQuests);
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error getting all quests for game {gameId}: {ex.Message}");
                return StatusCode(500, $"Error retrieving quests: {ex.Message}");
            }
        }

        [HttpGet("activeQuests")]
        public async Task<IActionResult> GetActiveQuests(string gameId)
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
                
                var activeQuests = await _storageService.GetActiveQuestsAsync(gameId, player.ActiveQuests);
                var simplifiedQuests = activeQuests.Select(q => new
                {
                    Id = q.Id,
                    Title = q.Title,
                    Objective = q.CoreObjective
                }).ToList();
                
                return Ok(simplifiedQuests);
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error getting active quests for game {gameId}: {ex.Message}");
                return StatusCode(500, $"Error retrieving active quests: {ex.Message}");
            }
        }

        [HttpPost("activateQuest")]
        public async Task<IActionResult> ActivateQuest(string gameId, string questId)
        {
            if (string.IsNullOrEmpty(gameId) || string.IsNullOrEmpty(questId))
            {
                return BadRequest("GameId and QuestId are required");
            }
            
            try
            {
                var player = await _storageService.GetPlayerAsync(gameId);
                
                if (player == null)
                {
                    return BadRequest("Player data not found. Character may not have been created yet.");
                }
                
                // Check if the quest exists
                var quest = await _storageService.GetQuestAsync(gameId, questId);
                if (quest == null)
                {
                    return BadRequest($"Quest with ID {questId} not found");
                }
                
                // Check if the quest is already active
                if (player.ActiveQuests == null)
                {
                    player.ActiveQuests = new List<string>();
                }
                
                if (!player.ActiveQuests.Contains(questId))
                {
                    player.ActiveQuests.Add(questId);
                    await _storageService.SaveAsync(gameId, "player", player);
                    
                    return Ok(new { Message = $"Quest {questId} activated successfully" });
                }
                else
                {
                    return Ok(new { Message = $"Quest {questId} is already active" });
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error activating quest {questId} for game {gameId}: {ex.Message}");
                return StatusCode(500, $"Error activating quest: {ex.Message}");
            }
        }

        [HttpPost("deactivateQuest")]
        public async Task<IActionResult> DeactivateQuest(string gameId, string questId)
        {
            if (string.IsNullOrEmpty(gameId) || string.IsNullOrEmpty(questId))
            {
                return BadRequest("GameId and QuestId are required");
            }
            
            try
            {
                var player = await _storageService.GetPlayerAsync(gameId);
                
                if (player == null)
                {
                    return BadRequest("Player data not found. Character may not have been created yet.");
                }
                
                // Check if the quest is active
                if (player.ActiveQuests != null && player.ActiveQuests.Contains(questId))
                {
                    player.ActiveQuests.Remove(questId);
                    await _storageService.SaveAsync(gameId, "player", player);
                    
                    return Ok(new { Message = $"Quest {questId} deactivated successfully" });
                }
                else
                {
                    return Ok(new { Message = $"Quest {questId} is not active" });
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error deactivating quest {questId} for game {gameId}: {ex.Message}");
                return StatusCode(500, $"Error deactivating quest: {ex.Message}");
            }
        }

        [HttpGet("npcConversationLog")]
        public async Task<IActionResult> GetNpcConversationLog(string gameId, string npcId)
        {
            if (string.IsNullOrEmpty(gameId) || string.IsNullOrEmpty(npcId))
            {
                return BadRequest("GameId and NpcId are required");
            }
            
            try
            {
                var npc = await _storageService.GetNpcAsync(gameId, npcId);
                
                if (npc == null)
                {
                    return BadRequest($"NPC with ID {npcId} not found");
                }
                
                return Ok(npc.ConversationLog);
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error getting conversation log for NPC {npcId} in game {gameId}: {ex.Message}");
                return StatusCode(500, $"Error retrieving conversation log: {ex.Message}");
            }
        }
    }
} 