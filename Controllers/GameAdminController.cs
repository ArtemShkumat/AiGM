using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AiGMBackEnd.Models;
using AiGMBackEnd.Services;
using AiGMBackEnd.Services.Storage;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using AiGMBackEnd.Models.Prompts;
using Hangfire;

namespace AiGMBackEnd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GameAdminController : ControllerBase
    {
        // Add a class for the create game from template request
        public class CreateGameFromTemplateRequest
        {
            public string UserId { get; set; }
            public string TemplateId { get; set; }
            public string NewGameName { get; set; }
        }
        
        // Add a class for the new request
        public class GenerateTemplateFromTextRequest
        {
            public string TemplateName { get; set; }
            public string LargeTextInput { get; set; }
        }
        
        private readonly PresenterService _presenterService;
        private readonly LoggingService _loggingService;
        private readonly StorageService _storageService;
        private readonly IWorldSyncService _worldSyncService;
        private readonly HangfireJobsService _hangfireJobsService;
        private readonly IBackgroundJobClient _backgroundJobClient;

        public GameAdminController(
            PresenterService presenterService, 
            LoggingService loggingService, 
            StorageService storageService,
            IWorldSyncService worldSyncService,
            HangfireJobsService hangfireJobsService,
            IBackgroundJobClient backgroundJobClient)
        {
            _presenterService = presenterService;
            _loggingService = loggingService;
            _storageService = storageService;
            _worldSyncService = worldSyncService;
            _hangfireJobsService = hangfireJobsService;
            _backgroundJobClient = backgroundJobClient;
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
        
        [HttpPost("bootstrapGameFromSimplePrompt")]
        public async Task<IActionResult> BootstrapGameFromSimplePrompt([FromBody] CreateScenarioRequest request)
        {
            if (string.IsNullOrEmpty(request.ScenarioPrompt))
            {
                return BadRequest("Scenario prompt is required");
            }

            try
            {
                _loggingService.LogInfo($"Creating game from simple prompt: {request.ScenarioPrompt}");
                
                // Call the presenter service to handle game bootstrap with isStartingScenario flag
                var scenarioId = await _presenterService.BootstrapGameFromSimplePromptAsync(request, isStartingScenario: true);
                
                return Ok(new { 
                    Message = "Game bootstrap from simple prompt initiated", 
                    ScenarioId = scenarioId,
                    CheckStatusEndpoint = $"/api/EntityStatus/pending/{scenarioId}"
                });
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error bootstrapping game from simple prompt: {ex.Message}");
                return StatusCode(500, new { Message = "An unexpected error occurred during game bootstrap", Error = ex.Message });
            }
        }

        [HttpPost("generate-template-from-text")]
        public async Task<IActionResult> GenerateTemplateFromText([FromBody] GenerateTemplateFromTextRequest request)
        {
            if (string.IsNullOrEmpty(request.TemplateName))
            {
                return BadRequest("Template name is required");
            }

            if (string.IsNullOrEmpty(request.LargeTextInput))
            {
                return BadRequest("Text input is required");
            }

            try
            {
                _loggingService.LogInfo($"Generating scenario template from text: {request.TemplateName}");
                
                // Generate a template ID
                string templateId = Guid.NewGuid().ToString();
                
                // Enqueue the job
                string jobId = _backgroundJobClient.Enqueue<HangfireJobsService>(x => 
                    x.GenerateScenarioTemplateAsync(request.LargeTextInput, templateId, request.TemplateName, null));
                
                return Accepted(new { 
                    Message = "Scenario template generation initiated", 
                    TemplateId = templateId,
                    JobId = jobId,
                    CheckStatusEndpoint = $"/api/EntityStatus/pending/{jobId}"
                });
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error generating scenario template: {ex.Message}");
                return StatusCode(500, new { Message = "An unexpected error occurred during template generation", Error = ex.Message });
            }
        }

        [HttpPost("create-game-from-template")]
        public async Task<IActionResult> CreateGameFromTemplate([FromBody] CreateGameFromTemplateRequest request)
        {
            if (string.IsNullOrEmpty(request.UserId))
            {
                return BadRequest("User ID is required");
            }

            if (string.IsNullOrEmpty(request.TemplateId))
            {
                return BadRequest("Template ID is required");
            }

            if (string.IsNullOrEmpty(request.NewGameName))
            {
                return BadRequest("Game name is required");
            }

            try
            {
                _loggingService.LogInfo($"Creating game {request.NewGameName} from template {request.TemplateId} for user {request.UserId}");
                
                // Generate a new game ID
                string newGameId = Guid.NewGuid().ToString();
                
                // Enqueue the job
                string jobId = _backgroundJobClient.Enqueue<HangfireJobsService>(x => 
                    x.InstantiateScenarioFromTemplateAsync(request.UserId, request.TemplateId, newGameId, request.NewGameName, null));
                
                return Ok(new { 
                    Message = "Game creation from template initiated", 
                    GameId = newGameId,
                    JobId = jobId,
                    CheckStatusEndpoint = $"/api/EntityStatus/pending/{jobId}"
                });
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error creating game from template: {ex.Message}");
                return StatusCode(500, new { Message = "An unexpected error occurred during game creation", Error = ex.Message });
            }
        }
    }
} 