using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace AiGMBackEnd.Services.Processors
{
    public interface ISummarizePromptProcessor
    {
        Task ProcessSummaryAsync(string summary, string userId);
        
        /// <summary>
        /// Process a combat summary after combat has ended
        /// </summary>
        /// <param name="summary">The combat summary text</param>
        /// <param name="userId">The user ID</param>
        /// <param name="playerVictory">Whether the player won the combat</param>
        Task ProcessCombatSummaryAsync(string summary, string userId, bool playerVictory);
    }
} 