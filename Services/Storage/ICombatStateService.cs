using System.Threading.Tasks;
using AiGMBackEnd.Models;

namespace AiGMBackEnd.Services.Storage
{
    /// <summary>
    /// Interface for managing combat state persistence operations
    /// </summary>
    public interface ICombatStateService
    {
        /// <summary>
        /// Saves a combat state to storage
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="combatState">The combat state to save</param>
        Task SaveCombatStateAsync(string userId, CombatState combatState);
        
        /// <summary>
        /// Loads the active combat state for a user
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <returns>The combat state or null if not found</returns>
        Task<CombatState?> LoadCombatStateAsync(string userId);
        
        /// <summary>
        /// Deletes the combat state file for a user
        /// </summary>
        /// <param name="userId">The user ID</param>
        Task DeleteCombatStateAsync(string userId);
    }
} 