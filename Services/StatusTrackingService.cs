using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using AiGMBackEnd.Models;
using System.Collections.Generic;

namespace AiGMBackEnd.Services
{
    public class StatusTrackingService : IStatusTrackingService
    {
        private readonly ConcurrentDictionary<string, EntityCreationStatus> _entityStatuses = new ConcurrentDictionary<string, EntityCreationStatus>();
        private readonly LoggingService _loggingService;

        public StatusTrackingService(LoggingService loggingService)
        {
            _loggingService = loggingService;
        }

        public Task RegisterEntityCreationAsync(string userId, string entityId, string entityType)
        {
            var key = GetKey(userId, entityId);
            var status = new EntityCreationStatus
            {
                EntityId = entityId,
                EntityType = entityType,
                Status = "pending",
                CreatedAt = DateTime.UtcNow
            };

            if (_entityStatuses.TryAdd(key, status))
            {
                _loggingService.LogInfo($"Registered pending entity creation: {entityType} {entityId} for user {userId}");
            }
            else
            {
                _loggingService.LogWarning($"Entity creation already registered: {entityType} {entityId} for user {userId}");
            }

            return Task.CompletedTask;
        }

        public Task UpdateEntityStatusAsync(string userId, string entityId, string status, string message = null)
        {
            var key = GetKey(userId, entityId);
            
            if (_entityStatuses.TryGetValue(key, out var existing))
            {
                existing.Status = status;
                
                if (message != null)
                {
                    existing.ErrorMessage = message;
                }
                
                if (status == "complete" || status == "error")
                {
                    existing.CompletedAt = DateTime.UtcNow;
                }
                
                _loggingService.LogInfo($"Updated entity status: {existing.EntityType} {entityId} to {status}");
            }
            else
            {
                _loggingService.LogWarning($"Attempted to update status for unknown entity: {entityId}");
            }
            
            return Task.CompletedTask;
        }

        public Task<EntityCreationStatus> GetEntityStatusAsync(string userId, string entityId)
        {
            var key = GetKey(userId, entityId);
            
            if (_entityStatuses.TryGetValue(key, out var status))
            {
                return Task.FromResult(status);
            }
            
            return Task.FromResult<EntityCreationStatus>(null);
        }
        
        public Task<bool> HasPendingEntitiesAsync(string userId)
        {
            // Check if any entities for this user have a pending or checking status
            bool hasPending = _entityStatuses
                .Any(pair => 
                    pair.Key.StartsWith($"{userId}:") && 
                    (pair.Value.Status == "pending" || pair.Value.Status == "checking")
                );
                
            //_loggingService.LogInfo($"Checked pending entities for user {userId}: {(hasPending ? "Has pending" : "No pending")}");
            
            return Task.FromResult(hasPending);
        }

        public Task<IEnumerable<EntityCreationStatus>> GetAllPendingEntitiesAsync(string userId)
        {
            var pendingEntities = _entityStatuses
                .Where(pair => 
                    pair.Key.StartsWith($"{userId}:") && 
                    (pair.Value.Status == "pending" || pair.Value.Status == "checking")
                )
                .Select(pair => pair.Value)
                .ToList();
                
            _loggingService.LogInfo($"Retrieved {pendingEntities.Count} pending entities for user {userId}");
            
            return Task.FromResult<IEnumerable<EntityCreationStatus>>(pendingEntities);
        }

        private string GetKey(string userId, string entityId)
        {
            return $"{userId}:{entityId}";
        }
    }
} 