using System.Threading.Tasks;
using AiGMBackEnd.Models;
using AiGMBackEnd.Models.Prompts;
using AiGMBackEnd.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Threading;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;

namespace AiGMBackEnd.Services.Processors
{
    public class UpdateProcessor : IUpdateProcessor
    {
        private readonly StorageService _storageService;
        private readonly LoggingService _loggingService;
        private readonly IStatusTrackingService _statusTrackingService;
        private readonly ILocationProcessor _locationProcessor;
        private readonly IQuestProcessor _questProcessor;
        private readonly INPCProcessor _npcProcessor;
        private readonly IServiceProvider _serviceProvider;

        public UpdateProcessor(
            StorageService storageService,
            LoggingService loggingService,
            IStatusTrackingService statusTrackingService,
            IServiceProvider serviceProvider,
            ILocationProcessor locationProcessor,
            IQuestProcessor questProcessor,
            INPCProcessor npcProcessor)
        {
            _storageService = storageService;
            _loggingService = loggingService;
            _statusTrackingService = statusTrackingService;
            _serviceProvider = serviceProvider;
            _locationProcessor = locationProcessor;
            _questProcessor = questProcessor;
            _npcProcessor = npcProcessor;
        }

        public async Task ProcessUpdatesAsync(JObject updates, string userId)
        {
            try
            {
                _loggingService.LogInfo("Processing updates");
                
                // Dictionary to track entity creation tasks
                var entityCreationJobs = new Dictionary<string, string>();
                
                // Step 1: Process all new entities first
                if (updates["newEntities"] is JArray newEntities && newEntities.Any())
                {
                    entityCreationJobs = await ProcessNewEntitiesAsync(newEntities, userId);
                }
                
                // Step 2: Process partial updates, waiting for dependencies if needed
                if (updates["partialUpdates"] is JObject partialUpdates)
                {
                    await ProcessPartialUpdatesAsync(partialUpdates, userId, entityCreationJobs);
                }
                
                _loggingService.LogInfo("Updates processing complete");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error processing updates: {ex.Message}");
                throw;
            }
        }

        private async Task<Dictionary<string, string>> ProcessNewEntitiesAsync(JArray newEntities, string userId)
        {
            var entityCreationJobs = new Dictionary<string, string>();
            
            foreach (var entity in newEntities)
            {
                string entityType = entity["type"]?.ToString()?.ToLower() ?? "";
                string entityId = entity["id"]?.ToString() ?? "";
                
                if (string.IsNullOrEmpty(entityId))
                {
                    _loggingService.LogWarning("Skipping entity with missing ID");
                    continue;
                }
                
                if (await EntityExistsInWorldAsync(userId, entityType, entityId))
                {
                    _loggingService.LogInfo($"Skipping creation of {entityType} entity: {entityId} - already exists");
                    continue;
                }
                
                await RegisterEntityInWorldAsync(userId, entity, entityType, entityId);
                
                // Queue entity creation using Hangfire
                string jobId = ScheduleEntityCreation(entity, entityType, userId);
                entityCreationJobs.Add(entityId, jobId);
            }
            
            return entityCreationJobs;
        }

        private async Task<bool> EntityExistsInWorldAsync(string userId, string entityType, string entityId)
        {
            var world = await _storageService.GetWorldAsync(userId);
            if (world == null) return false;
            
            return entityType switch
            {
                "npc" => world.Npcs?.Any(n => n.Id == entityId) ?? false,
                "location" => world.Locations?.Any(l => l.Id == entityId) ?? false,
                "quest" => world.Quests?.Any(q => q.Id == entityId) ?? false,
                _ => false
            };
        }

        private async Task RegisterEntityInWorldAsync(string userId, JToken entity, string entityType, string entityId)
        {
            string entityDisplayName = entityType == "quest" 
                ? entity["title"]?.ToString() ?? entity["name"]?.ToString() ?? "New Quest"
                : entity["name"]?.ToString() ?? "New Entity";
                
            await _storageService.AddEntityToWorldAsync(userId, entityId, entityDisplayName, entityType);
        }

        // Schedule entity creation using Hangfire
        private string ScheduleEntityCreation(JToken entity, string entityType, string userId)
        {
            var context = entity["context"]?.ToString() ?? "";
            var entityId = entity["id"]?.ToString() ?? "";
            var currentLocation = entity["currentLocationId"]?.ToString() ?? "";
            
            string jobId;
            
            // Create appropriate job based on entity type
            switch (entityType)
            {
                case "npc":
                    var npcName = entity["name"]?.ToString() ?? "New NPC";
                    jobId = BackgroundJob.Enqueue(() => 
                        _serviceProvider.GetService<HangfireJobsService>().CreateNpcAsync(userId, entityId, npcName, context, currentLocation));
                    _loggingService.LogInfo($"Scheduled NPC creation job for {entityId}, job ID: {jobId}");
                    break;
                
                case "location":
                    var locationName = entity["name"]?.ToString() ?? "New Location";
                    var locationType = entity["locationType"]?.ToString() ?? "Building";
                    jobId = BackgroundJob.Enqueue(() => 
                        _serviceProvider.GetService<HangfireJobsService>().CreateLocationAsync(userId, entityId, locationName, locationType, context));
                    _loggingService.LogInfo($"Scheduled location creation job for {entityId}, job ID: {jobId}");
                    break;
                
                case "quest":
                    var questName = entity["name"]?.ToString() ?? "New Quest";
                    jobId = BackgroundJob.Enqueue(() => 
                        _serviceProvider.GetService<HangfireJobsService>().CreateQuestAsync(userId, entityId, questName, context));
                    _loggingService.LogInfo($"Scheduled quest creation job for {entityId}, job ID: {jobId}");
                    break;
                
                default:
                    _loggingService.LogWarning($"Unknown entity type: {entityType}");
                    return string.Empty;
            }
            
            // Register this entity creation with status tracking service
            _statusTrackingService.RegisterEntityCreationAsync(userId, entityId, entityType);
            
            return jobId;
        }

        private async Task ProcessPartialUpdatesAsync(JObject partialUpdates, string userId, Dictionary<string, string> entityCreationJobs)
        {
            foreach (var property in partialUpdates.Properties())
            {
                var entityId = property.Name;
                if (!(property.Value is JObject updateData))
                {
                    _loggingService.LogWarning($"Invalid update data for entity {entityId}");
                    continue;
                }
                
                // Handle player special case
                if (entityId.ToLower() == "player")
                {
                    _loggingService.LogInfo("Processing player update");
                    
                    // Check if the player is changing locations
                    var previousPlayer = await _storageService.GetPlayerAsync(userId);
                    string previousLocationId = previousPlayer?.CurrentLocationId;
                    
                    // Check if there's a currentLocationId in the update data
                    string newLocationId = null;
                    var currentLocationProperty = updateData["currentLocationId"];
                    if (currentLocationProperty != null)
                    {
                        newLocationId = currentLocationProperty.ToString();
                    }
                    
                    // If the player had a previous location and is now moving to a different one,
                    // we should summarize the conversation from the previous location
                    if (!string.IsNullOrEmpty(previousLocationId) && 
                        (string.IsNullOrEmpty(newLocationId) || previousLocationId != newLocationId))
                    {
                        _loggingService.LogInfo($"Player leaving location {previousLocationId}. Triggering conversation summarization.");
                        await _serviceProvider.GetService<HangfireJobsService>().SummarizeConversationAsync(userId);
                    }
                    
                    await UpdateEntityAsync(userId, "", "player", updateData.ToString());
                    _loggingService.LogInfo("Applied partial update to player");
                    continue;
                }
                
                // Get the entity type from the update data
                var entityType = updateData["type"]?.ToString();
                if (string.IsNullOrEmpty(entityType))
                {
                    _loggingService.LogWarning($"Entity type missing for {entityId}");
                    continue;
                }
                
                // Remove ID and Type from update data to preserve them
                updateData.Remove("id");
                updateData.Remove("type");
                
                // Check if this entity is currently being created
                if (entityCreationJobs.TryGetValue(entityId, out string jobId))
                {
                    // Wait for the entity to be created before applying the update
                    _loggingService.LogInfo($"Waiting for {entityType} {entityId} to be created before applying update");
                    try
                    {
                        // Instead of waiting directly for the job, we'll check the status
                        // periodically (with a timeout) through the status tracking service
                        var timeoutTask = Task.Delay(TimeSpan.FromMinutes(2));
                        var checkInterval = TimeSpan.FromSeconds(2);
                        var startTime = DateTime.UtcNow;
                        bool isComplete = false;
                        
                        // Poll every few seconds to check if the entity creation is complete
                        while (!isComplete && DateTime.UtcNow - startTime < TimeSpan.FromMinutes(2))
                        {
                            var status = await _statusTrackingService.GetEntityStatusAsync(userId, entityId);
                            if (status != null && (status.Status == "complete" || status.Status == "error"))
                            {
                                isComplete = true;
                                if (status.Status == "complete")
                                {
                                    _loggingService.LogInfo($"Entity {entityType} {entityId} creation completed");
                                }
                                else
                                {
                                    _loggingService.LogWarning($"Entity {entityType} {entityId} creation failed: {status.ErrorMessage}");
                                }
                                break;
                            }
                            
                            await Task.Delay(checkInterval);
                        }
                        
                        if (!isComplete)
                        {
                            _loggingService.LogWarning($"Timed out waiting for {entityType} {entityId} creation, proceeding with update anyway");
                        }
                    }
                    catch (Exception ex)
                    {
                        _loggingService.LogError($"Error checking creation status for {entityType} {entityId}: {ex.Message}");
                        // Proceed with update anyway
                    }
                }
                
                // Determine collection based on the entity type
                string collection = entityType.ToLower() switch
                {
                    "npc" => "npcs",
                    "location" => "locations",
                    "quest" => "quests",
                    "world" => "",
                    "player" => "",
                    _ => null
                };
                
                if (collection == null)
                {
                    _loggingService.LogWarning($"Unknown entity type: {entityType} for entity {entityId}");
                    continue;
                }
                
                // Apply the update
                await UpdateEntityAsync(userId, collection, entityId, updateData.ToString());
            }
        }
        
        private async Task UpdateEntityAsync(string userId, string entityType, string entityId, string updateData)
        {
            var filePath = string.IsNullOrEmpty(entityType) 
                ? entityId 
                : $"{entityType}/{entityId}";
                
            try 
            {
                await _storageService.ApplyPartialUpdateAsync(userId, filePath, updateData);
                _loggingService.LogInfo($"Updated {entityType} entity: {entityId}");
            }
            catch (System.IO.FileNotFoundException)
            {
                _loggingService.LogWarning($"Cannot apply partial update to non-existent file: {filePath}");
                _loggingService.LogInfo($"Updated {entityType} entity: {entityId}");
            }
        }
    }
} 