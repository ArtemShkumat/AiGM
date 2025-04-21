using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace AiGMBackEnd.Services.Processors
{
    public interface IScenarioProcessor
    {
        Task ProcessAsync(JObject scenarioData, string scenarioId, string userId, bool isStartingScenario = false);
    }
} 