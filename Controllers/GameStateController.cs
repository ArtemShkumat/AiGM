using System;
using System.Threading.Tasks;
using AiGMBackEnd.Models;
using AiGMBackEnd.Services;
using Microsoft.AspNetCore.Mvc;

namespace AiGMBackEnd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GameStateController : ControllerBase
    {
        private readonly LoggingService _loggingService;
        private readonly StorageService _storageService;

        public GameStateController(LoggingService loggingService, StorageService storageService)
        {
            _loggingService = loggingService;
            _storageService = storageService;
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
} 