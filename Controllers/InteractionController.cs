using System;
using System.Threading.Tasks;
using AiGMBackEnd.Models;
using AiGMBackEnd.Services;
using Microsoft.AspNetCore.Mvc;

namespace AiGMBackEnd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InteractionController : ControllerBase
    {
        private readonly PresenterService _presenterService;
        private readonly LoggingService _loggingService;
        private readonly StorageService _storageService;

        public InteractionController(PresenterService presenterService, LoggingService loggingService, StorageService storageService)
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
                var response = await _presenterService.HandleUserInputAsync(request.GameId, request.UserInput, request.PromptType, request.NpcId);
                
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
    }
} 