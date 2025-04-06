using System.Collections.Generic;
using System.Threading.Tasks;
using AiGMBackEnd.Models;

namespace AiGMBackEnd.Services.Storage
{
    public interface IGameScenarioService
    {
        List<string> GetScenarioIds();
        Task<T> LoadScenarioSettingAsync<T>(string scenarioId, string fileId) where T : class;
        Task<string> CreateGameFromScenarioAsync(string scenarioId, GamePreferences preferences = null);
        List<string> GetGameIds();
    }
} 