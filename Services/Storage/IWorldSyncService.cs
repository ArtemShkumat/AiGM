using System.Collections.Generic;
using System.Threading.Tasks;

namespace AiGMBackEnd.Services.Storage
{
    public interface IWorldSyncService
    {
        Task SyncWorldWithEntitiesAsync(string userId);
        
        /// <summary>
        /// Synchronizes NPCs with their locations to ensure location.Npcs arrays contain the NPCs that reference them
        /// </summary>
        /// <param name="gameId">The game/user ID</param>
        /// <returns>Detailed results of the synchronization process</returns>
        Task<(int UpdatedCount, List<object> SyncResults)> SyncNpcLocationsAsync(string gameId);
    }
} 