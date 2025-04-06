using System.Threading.Tasks;
using AiGMBackEnd.Models;
using System.Collections.Generic;

namespace AiGMBackEnd.Services
{
    public interface IStatusTrackingService
    {
        Task RegisterEntityCreationAsync(string userId, string entityId, string entityType);
        Task UpdateEntityStatusAsync(string userId, string entityId, string status, string message = null);
        Task<EntityCreationStatus> GetEntityStatusAsync(string userId, string entityId);
        Task<bool> HasPendingEntitiesAsync(string userId);
        Task<IEnumerable<EntityCreationStatus>> GetAllPendingEntitiesAsync(string userId);
    }
} 