using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AiGMBackEnd.Models;
using AiGMBackEnd.Services;
using AiGMBackEnd.Services.Storage;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace AiGMBackEnd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GameAdminController : ControllerBase
    {
        private readonly PresenterService _presenterService;
        private readonly LoggingService _loggingService;
        private readonly StorageService _storageService;
        private readonly IWorldSyncService _worldSyncService;

        public GameAdminController(
            PresenterService presenterService, 
            LoggingService loggingService, 
            StorageService storageService,
            IWorldSyncService worldSyncService)
        {
            _presenterService = presenterService;
            _loggingService = loggingService;
            _storageService = storageService;
            _worldSyncService = worldSyncService;
        }

        [HttpGet("{gameId}/validate")]
        public async Task<IActionResult> ValidateGameData(string gameId)
        {
            if (string.IsNullOrEmpty(gameId))
            {
                return BadRequest("GameId is required");
            }

            try
            {
                _loggingService.LogInfo($"Starting validation for game: {gameId}");
                var danglingRefs = await _storageService.FindDanglingReferencesAsync(gameId);

                if (danglingRefs == null || !danglingRefs.Any())
                {
                    _loggingService.LogInfo($"Validation complete for game {gameId}. No dangling references found.");
                    return Ok(new { Message = "Validation successful. No dangling references found.", DanglingReferences = new List<StorageService.DanglingReferenceInfo>() });
                }
                else
                {
                    // Log details of each dangling reference
                    foreach (var dr in danglingRefs)
                    {
                        _loggingService.LogWarning($"Validation for game {gameId}: Found dangling reference '{dr.ReferenceId}' of type '{dr.ReferenceType}' in file '{dr.FilePath}'");
                    }
                    return Ok(new { Message = "Validation finished. Found dangling references.", DanglingReferences = danglingRefs });
                }
            }
            catch (DirectoryNotFoundException dnfe)
            {
                _loggingService.LogWarning($"Validation failed for game {gameId}. Game directory not found: {dnfe.Message}");
                return NotFound(new { Message = $"Validation failed. Game with ID '{gameId}' not found." });
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error during validation for game {gameId}: {ex.Message}");
                return StatusCode(500, new { Message = $"An unexpected error occurred during validation.", Error = ex.Message });
            }
        }
        
        [HttpPost("{gameId}/autocreate-dangling")]
        public async Task<IActionResult> AutoCreateDanglingReferences(string gameId)
        {
            if (string.IsNullOrEmpty(gameId))
            {
                return BadRequest("GameId is required");
            }

            try
            {
                _loggingService.LogInfo($"Starting auto-creation of dangling references for game: {gameId}");
                
                // Call the presenter service to handle the auto-creation
                int createdCount = await _presenterService.AutoCreateDanglingReferencesAsync(gameId);
                
                return Ok(new { 
                    Message = $"Auto-creation of dangling references initiated. {createdCount} entities queued for creation.", 
                    EntitiesCreated = createdCount,
                    CheckStatusEndpoint = $"/api/EntityStatus/pending/{gameId}"
                });
            }
            catch (DirectoryNotFoundException dnfe)
            {
                _loggingService.LogWarning($"Auto-creation failed for game {gameId}. Game directory not found: {dnfe.Message}");
                return NotFound(new { Message = $"Auto-creation failed. Game with ID '{gameId}' not found." });
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error during auto-creation for game {gameId}: {ex.Message}");
                return StatusCode(500, new { Message = $"An unexpected error occurred during auto-creation.", Error = ex.Message });
            }
        }
        
        [HttpPost("{gameId}/sync-npc-locations")]
        public async Task<IActionResult> SyncNpcLocations(string gameId)
        {
            if (string.IsNullOrEmpty(gameId))
            {
                return BadRequest("GameId is required");
            }

            try
            {
                // Call the service to perform the synchronization
                var (updatedCount, syncResults) = await _worldSyncService.SyncNpcLocationsAsync(gameId);
                
                return Ok(new { 
                    Message = $"NPC location synchronization complete. Updated {updatedCount} locations.", 
                    UpdatedCount = updatedCount,
                    Details = syncResults 
                });
            }
            catch (DirectoryNotFoundException dnfe)
            {
                _loggingService.LogWarning($"Synchronization failed for game {gameId}. Game directory not found: {dnfe.Message}");
                return NotFound(new { Message = $"Synchronization failed. Game with ID '{gameId}' not found." });
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error during NPC location synchronization for game {gameId}: {ex.Message}");
                return StatusCode(500, new { Message = $"An unexpected error occurred during synchronization.", Error = ex.Message });
            }
        }
    }
} 