using System.Collections.Generic;
using System.Threading.Tasks;
using AiGMBackEnd.Models;
using Newtonsoft.Json.Linq;

namespace AiGMBackEnd.Services.Storage
{
    /// <summary>
    /// Service for managing starting scenarios (templates)
    /// </summary>
    public interface IGameScenarioService
    {
        List<string> GetScenarioIds();
        Task<T> LoadScenarioSettingAsync<T>(string scenarioId, string fileId) where T : class;
        Task<string> CreateGameFromScenarioAsync(string scenarioId, GamePreferences preferences = null);
        List<string> GetGameIds();
        
        // Scenario file operation methods (only for starting scenarios)
        /// <summary>
        /// Creates folder structure for a starting scenario
        /// </summary>
        /// <param name="scenarioId">The scenario ID</param>
        /// <param name="userId">Not used, as GameScenarioService only deals with starting scenarios</param>
        /// <param name="isStartingScenario">Not used, always true for GameScenarioService</param>
        Task CreateScenarioFolderStructureAsync(string scenarioId, string userId, bool isStartingScenario);
        
        /// <summary>
        /// Saves a file for a starting scenario
        /// </summary>
        /// <param name="scenarioId">The scenario ID</param>
        /// <param name="fileName">The file name to save</param>
        /// <param name="jsonData">The JSON data to save</param>
        /// <param name="userId">Not used, as GameScenarioService only deals with starting scenarios</param>
        /// <param name="isStartingScenario">Not used, always true for GameScenarioService</param>
        Task SaveScenarioFileAsync(string scenarioId, string fileName, JToken jsonData, string userId, bool isStartingScenario);
        
        /// <summary>
        /// Saves a location file for a starting scenario
        /// </summary>
        /// <param name="scenarioId">The scenario ID</param>
        /// <param name="locationId">The location ID</param>
        /// <param name="locationData">The location data to save</param>
        /// <param name="userId">Not used, as GameScenarioService only deals with starting scenarios</param>
        /// <param name="isStartingScenario">Not used, always true for GameScenarioService</param>
        Task SaveScenarioLocationAsync(string scenarioId, string locationId, JToken locationData, string userId, bool isStartingScenario);
        
        /// <summary>
        /// Saves an NPC file for a starting scenario
        /// </summary>
        /// <param name="scenarioId">The scenario ID</param>
        /// <param name="npcId">The NPC ID</param>
        /// <param name="npcData">The NPC data to save</param>
        /// <param name="userId">Not used, as GameScenarioService only deals with starting scenarios</param>
        /// <param name="isStartingScenario">Not used, always true for GameScenarioService</param>
        Task SaveScenarioNpcAsync(string scenarioId, string npcId, JToken npcData, string userId, bool isStartingScenario);
    }
} 