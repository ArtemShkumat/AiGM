using System.Threading.Tasks;
using AiGMBackEnd.Models;

namespace AiGMBackEnd.Services.Storage
{
    /// <summary>
    /// Interface for managing enemy stat block persistence operations
    /// </summary>
    public interface IEnemyStatBlockService
    {
        /// <summary>
        /// Loads an EnemyStatBlock from storage
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="enemyId">The enemy ID</param>
        /// <returns>The enemy stat block or null if not found</returns>
        Task<EnemyStatBlock?> LoadEnemyStatBlockAsync(string userId, string enemyId);
        
        /// <summary>
        /// Saves an EnemyStatBlock to storage
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="statBlock">The enemy stat block to save</param>
        Task SaveEnemyStatBlockAsync(string userId, EnemyStatBlock statBlock);
        
        /// <summary>
        /// Checks if an EnemyStatBlock exists in storage
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="enemyId">The enemy ID</param>
        /// <returns>True if the enemy stat block exists, false otherwise</returns>
        Task<bool> CheckIfStatBlockExistsAsync(string userId, string enemyId);
    }
} 