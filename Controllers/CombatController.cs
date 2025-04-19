using System;
using System.Threading.Tasks;
using AiGMBackEnd.Models;
using AiGMBackEnd.Services;
using Azure;
using Microsoft.AspNetCore.Mvc;

namespace AiGMBackEnd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CombatController : ControllerBase
    {
        private readonly StorageService _storageService;
        private readonly LoggingService _loggingService;
        private readonly GameNotificationService _gameNotificationService;
        private readonly PresenterService _presenterService;

        public CombatController(
            StorageService storageService,
            LoggingService loggingService,
            GameNotificationService gameNotificationService,
            PresenterService presenterService)
        {
            _storageService = storageService;
            _loggingService = loggingService;
            _gameNotificationService = gameNotificationService;
            _presenterService = presenterService;
        }

        /// <summary>
        /// Start a combat encounter with a specified enemy
        /// </summary>
        [HttpPost("start")]
        public async Task<IActionResult> StartCombat([FromBody] StartCombatRequest request)
        {
            if (string.IsNullOrEmpty(request.GameId))
            {
                return BadRequest("GameId is required");
            }

            if (string.IsNullOrEmpty(request.EnemyId))
            {
                return BadRequest("EnemyId is required");
            }

            try
            {
                // Check if there's already an active combat for this game
                var existingCombat = await _storageService.LoadCombatStateAsync(request.GameId);
                if (existingCombat != null && existingCombat.IsActive)
                {
                    return Conflict(new {
                        Success = false,
                        Error = "Combat is already in progress for this game"
                    });
                }

                // Check if enemy stat block exists
                bool enemyExists = await _storageService.CheckIfStatBlockExistsAsync(request.GameId, request.EnemyId);
                if (!enemyExists)
                {
                    return NotFound(new {
                        Success = false,
                        Error = $"Enemy stat block not found for {request.EnemyId}"
                    });
                }

                // Load enemy stat block
                var enemyStatBlock = await _storageService.LoadEnemyStatBlockAsync(request.GameId, request.EnemyId);
                if (enemyStatBlock == null)
                {
                    return NotFound(new {
                        Success = false,
                        Error = $"Failed to load enemy stat block for {request.EnemyId}"
                    });
                }

                // Create new combat state
                var combatState = new CombatState
                {
                    CombatId = Guid.NewGuid().ToString(),
                    UserId = request.GameId,
                    EnemyStatBlockId = enemyStatBlock.Id,
                    CurrentEnemySuccesses = 0,
                    PlayerConditions = new List<string>(),
                    CombatLog = new List<string>(),
                    IsActive = true
                };

                // Save combat state
                await _storageService.SaveCombatStateAsync(request.GameId, combatState);

                // Notify clients that combat has started
                await _gameNotificationService.NotifyCombatStartedAsync(request.GameId, new CombatStartInfo
                {
                    CombatId = combatState.CombatId,
                    EnemyId = enemyStatBlock.Id,
                    EnemyName = enemyStatBlock.Name,
                    EnemyDescription = enemyStatBlock.Description,
                    EnemyLevel = enemyStatBlock.Level,
                    SuccessesRequired = enemyStatBlock.SuccessesRequired,
                    PlayerConditions = combatState.PlayerConditions
                });

                return Ok(new {
                    Success = true,
                    CombatId = combatState.CombatId,
                    Message = $"Combat started against {enemyStatBlock.Name}"
                });
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error starting combat: {ex.Message}");
                return StatusCode(500, new {
                    Success = false,
                    Error = $"Error starting combat: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Submit a player action during combat
        /// </summary>
        [HttpPost("action")]
        public async Task<IActionResult> SubmitCombatAction([FromBody] CombatActionRequest request)
        {
            if (string.IsNullOrEmpty(request.GameId))
            {
                return BadRequest("GameId is required");
            }

            if (string.IsNullOrEmpty(request.Action))
            {
                return BadRequest("Action is required");
            }

            try
            {
                // Ensure there IS an active combat state before allowing actions
                var combatState = await _storageService.LoadCombatStateAsync(request.GameId);
                if (combatState == null || !combatState.IsActive)
                {
                    return NotFound(new {
                        Success = false,
                        Error = "No active combat found for this game. Cannot process action."
                    });
                }

                // Call PresenterService to handle the user input with PromptType.Combat
                // This will enqueue a Hangfire job using HangfireJobsService.ProcessUserInputAsync
                var response = await _presenterService.HandleUserInputAsync(
                    request.GameId, 
                    request.Action, // Use the 'Action' field from CombatActionRequest
                    PromptType.Combat);

                // Return the JobId so the frontend can poll for the result
                return Ok(new UserInputResponse
                {
                    Response = response,
                    Success = true
                });
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error processing combat action for game {request.GameId}: {ex.Message}");
                return StatusCode(500, new {
                    Success = false,
                    Error = $"An error occurred while processing your combat action: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// End a combat encounter (victory or defeat)
        /// </summary>
        [HttpPost("end")]
        public async Task<IActionResult> EndCombat([FromBody] EndCombatRequest request)
        {
            if (string.IsNullOrEmpty(request.GameId))
            {
                return BadRequest("GameId is required");
            }

            try
            {
                // Load current combat state
                var combatState = await _storageService.LoadCombatStateAsync(request.GameId);
                if (combatState == null)
                {
                    return NotFound(new {
                        Success = false,
                        Error = "No combat found for this game"
                    });
                }

                if (!combatState.IsActive)
                {
                    return BadRequest(new {
                        Success = false,
                        Error = "Combat is already ended"
                    });
                }

                // Mark combat as inactive
                combatState.IsActive = false;
                
                // Save updated combat state
                await _storageService.SaveCombatStateAsync(request.GameId, combatState);

                // Notify clients that combat has ended
                await _gameNotificationService.NotifyCombatEndedAsync(request.GameId, request.PlayerVictory);

                // Delete combat state if requested
                if (request.CleanupData)
                {
                    await _storageService.DeleteCombatStateAsync(request.GameId);
                }

                return Ok(new {
                    Success = true,
                    Message = request.PlayerVictory ? "Combat ended with player victory" : "Combat ended with player defeat"
                });
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error ending combat: {ex.Message}");
                return StatusCode(500, new {
                    Success = false,
                    Error = $"Error ending combat: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Get the current combat state
        /// </summary>
        [HttpGet("{gameId}")]
        public async Task<IActionResult> GetCombatState(string gameId)
        {
            if (string.IsNullOrEmpty(gameId))
            {
                return BadRequest("GameId is required");
            }

            try
            {
                var combatState = await _storageService.LoadCombatStateAsync(gameId);
                if (combatState == null)
                {
                    return NotFound(new {
                        Success = false,
                        Error = "No combat found for this game"
                    });
                }

                // If combat exists but is inactive
                if (!combatState.IsActive)
                {
                    return Ok(new {
                        Success = true,
                        IsActive = false,
                        Message = "Combat exists but is no longer active"
                    });
                }

                // Load enemy stat block for additional info
                var enemyStatBlock = await _storageService.LoadEnemyStatBlockAsync(gameId, combatState.EnemyStatBlockId);
                if (enemyStatBlock == null)
                {
                    return NotFound(new {
                        Success = false,
                        Error = $"Failed to load enemy stat block for {combatState.EnemyStatBlockId}"
                    });
                }

                return Ok(new {
                    Success = true,
                    IsActive = true,
                    CombatId = combatState.CombatId,
                    EnemyId = enemyStatBlock.Id,
                    EnemyName = enemyStatBlock.Name,
                    EnemyLevel = enemyStatBlock.Level,
                    CurrentEnemySuccesses = combatState.CurrentEnemySuccesses,
                    SuccessesRequired = enemyStatBlock.SuccessesRequired,
                    PlayerConditions = combatState.PlayerConditions,
                    CombatLog = combatState.CombatLog
                });
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error getting combat state: {ex.Message}");
                return StatusCode(500, new {
                    Success = false,
                    Error = $"Error getting combat state: {ex.Message}"
                });
            }
        }
    }

    // Request/response models for the combat endpoints
    public class StartCombatRequest
    {
        public string GameId { get; set; }
        public string EnemyId { get; set; }
    }

    public class CombatActionRequest
    {
        public string GameId { get; set; }
        public string Action { get; set; }
        public List<string> PlayerTags { get; set; } = new List<string>();
    }

    public class EndCombatRequest
    {
        public string GameId { get; set; }
        public bool PlayerVictory { get; set; }
        public bool CleanupData { get; set; } = false;
    }
} 