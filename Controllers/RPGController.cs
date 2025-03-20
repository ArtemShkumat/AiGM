using System.Threading.Tasks;
using AiGMBackEnd.Models;
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
        public IActionResult GetScenarios()
        {
            var scenarios = new List<ScenarioInfo> {
                //load from data/startingScenarios. You can use the name of the startingScenario folder as the scenarioId
            };
            return Ok(scenarios);
        }

        [HttpPost("createGame")]
        public IActionResult CreateNewGame([FromBody] NewGameRequest req)
        {
            // e.g. create a game folder in Data/userData with folder name as gameId
            // copy or reference the prebuilt scenario JSON files, etc.
            // generate a gameId

            var gameId = Guid.NewGuid().ToString();

            // Save scenario + preferences to some store if needed
            // or do PresenterService.NewGame(...) logic

            return Ok(new { gameId });
        }

        [HttpGet("listGames")]
        public IActionResult ListGames()
        {
            //Get all the games from data/userData and return the list of their ids, and names.
            return Ok();//and a list of games
        }

        [HttpPost("createCharacter")]
        public IActionResult CreateCharacter(string characterDescription)
        {
            //Call presenter service, pass it characterDescription and tell it to create a job using "createPlayerJson" template. We'll add createPlayerJson template soon.
            return Ok();
        }

        [HttpGet("scene")]
        public IActionResult Scene(string gameId)
        {
            //Call storage service to return the list of NPC ids and Names that have visibleToPlayer: true. We may expand this later to return other scene elements.
            return Ok();
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
        public string Description { get; set; }
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
}
