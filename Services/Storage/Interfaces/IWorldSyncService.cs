using System.Collections.Generic;
using System.Threading.Tasks;

namespace AiGMBackEnd.Services.Storage
{
    public interface IWorldSyncService
    {
        Task SyncWorldWithEntitiesAsync(string userId);        
    }
} 