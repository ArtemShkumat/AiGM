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
        
        /// <summary>
        /// Updates all NPCs in a specific location to set their VisibleToPlayer property to false
        /// This is typically called after a player leaves a location and a conversation is summarized
        /// </summary>
        /// <param name="userId">The user/game ID</param>
        /// <param name="locationId">The location ID to process</param>
        /// <returns>The number of NPCs that were updated</returns>
        Task<int> HideNpcsInLocationAsync(string userId, string locationId);
    }
} 