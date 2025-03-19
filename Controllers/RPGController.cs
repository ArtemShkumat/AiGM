using System.Threading.Tasks;
using AiGMBackEnd.Services;
using Microsoft.AspNetCore.Mvc;

namespace AiGMBackEnd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RPGController : ControllerBase
    {
        private readonly PresenterService _presenterService;
        private readonly LoggingService _loggingService;

        public RPGController(PresenterService presenterService, LoggingService loggingService)
        {
            _presenterService = presenterService;
            _loggingService = loggingService;
        }

        [HttpPost("input")]
        public async Task<IActionResult> ProcessUserInput([FromBody] UserInputRequest request)
        {
            if (string.IsNullOrEmpty(request.UserId))
            {
                return BadRequest("UserId is required");
            }

            if (string.IsNullOrEmpty(request.UserInput))
            {
                return BadRequest("UserInput is required");
            }

            try
            {
                var response = await _presenterService.HandleUserInputAsync(request.UserId, request.UserInput, request.PromptType);
                
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
    }

    public class UserInputRequest
    {
        public string UserId { get; set; }
        public string UserInput { get; set; }
        public PromptType PromptType { get; set; } = PromptType.DM; // Default to DM prompt
    }

    public class UserInputResponse
    {
        public string Response { get; set; }
        public bool Success { get; set; }
        public string Error { get; set; }
    }
}
