using System.Threading.Tasks;
using AiGMBackEnd.Models;
using AiGMBackEnd.Models.Prompts;
using AiGMBackEnd.Services;
using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace AiGMBackEnd.Services.Processors
{
    public class UpdateProcessor
    {
        private readonly StorageService _storageService;
        private readonly LoggingService _loggingService;
        private readonly BackgroundJobService _backgroundJobService;
        private readonly LocationProcessor _locationProcessor;
        private readonly QuestProcessor _questProcessor;
        private readonly NPCProcessor _npcProcessor;

        public UpdateProcessor(
            StorageService storageService,
            LoggingService loggingService,
            BackgroundJobService backgroundJobService)
        {
            _storageService = storageService;
            _loggingService = loggingService;
            _backgroundJobService = backgroundJobService;
            _locationProcessor = new LocationProcessor(storageService, loggingService);
            _questProcessor = new QuestProcessor(storageService, loggingService);
            _npcProcessor = new NPCProcessor(storageService, loggingService);
        }

        public async Task ProcessUpdatesAsync(JObject updates, string userId)
        {
            try
            {
                _loggingService.LogInfo("Processing updates");
                
                // Process new entities
                if (updates["newEntities"] != null)
                {
                    var newEntities = updates["newEntities"] as JArray;
                    if (newEntities != null)
                    {
                        foreach (var entity in newEntities)
                        {
                            string entityType = entity["type"]?.ToString()?.ToLower() ?? "";
                            string entityId = entity["id"]?.ToString() ?? "";
                            string currentLocation = entity["currentLocationId"]?.ToString() ?? "";
                            string entityName = entity["name"]?.ToString() ?? "";
                            string context = entity["context"]?.ToString() ?? "";

                            if (string.IsNullOrEmpty(entityId))
                            {
                                _loggingService.LogWarning("Skipping entity with missing ID");
                                continue;
                            }

                            // Get world object to check if entity already exists
                            var world = await _storageService.GetWorldAsync(userId);
                            if (world != null)
                            {
                                bool entityExists = false;
                                
                                // Check if entity already exists in the corresponding section of the world object
                                switch (entityType)
                                {
                                    case "npc":
                                        entityExists = world.Npcs?.Any(n => n.Id == entityId) ?? false;
                                        break;
                                    case "location":
                                        entityExists = world.Locations?.Any(l => l.Id == entityId) ?? false;
                                        break;
                                    case "quest":
                                        entityExists = world.Quests?.Any(q => q.Id == entityId) ?? false;
                                        break;
                                }
                                
                                if (entityExists)
                                {
                                    _loggingService.LogInfo($"Skipping creation of {entityType} entity: {entityId} - already exists in world");
                                    continue;
                                }
                                else
                                {
                                    // Add the entity to the world before proceeding with its creation
                                    string entityDisplayName = entityName;
                                    
                                    // For quests, use "title" rather than "name"
                                    if (entityType == "quest")
                                    {
                                        entityDisplayName = entity["title"]?.ToString() ?? entityName;
                                    }
                                    
                                    await _storageService.AddEntityToWorldAsync(userId, entityId, entityDisplayName, entityType);
                                }
                            }
                                    
                            _loggingService.LogInfo($"Processing creation of {entityType} entity: {entityId}");
                                    
                            // First, save the basic entity data immediately
                            switch (entityType)
                            {
                                case "npc":
                                    var npcName = entity["name"]?.ToString() ?? "";
                                    _loggingService.LogInfo($"Will trigger separate job to create NPC: {npcName}");
                                    FireAndForgetEntityCreation(new PromptRequest 
                                    { 
                                        PromptType = PromptType.CreateNPC,
                                        UserId = userId,
                                        Context = context,
                                        NpcLocation = currentLocation,
                                        NpcName = npcName
                                    });
                                    break;
                                
                                case "location":
                                    var locationName = entity["name"]?.ToString() ?? "New Location";
                                    var locationType = entity["locationType"]?.ToString() ?? "Building";
                                    _loggingService.LogInfo($"Will trigger separate job to create location: {locationName}");
                                    FireAndForgetEntityCreation(new PromptRequest 
                                    { 
                                        PromptType = PromptType.CreateLocation,
                                        UserId = userId,
                                        LocationId = entityId,
                                        LocationName = locationName,
                                        LocationType = locationType,
                                        Context = context
                                    });
                                    break;
                                case "quest":                                    
                                    var questTitle = entity["name"]?.ToString() ?? "New Quest";
                                    _loggingService.LogInfo($"Will trigger separate job to create quest: {questTitle}");
                                    FireAndForgetEntityCreation(new PromptRequest 
                                    { 
                                        PromptType = PromptType.CreateQuest,
                                        UserId = userId,
                                        Context = context,
                                        QuestName = entityName,
                                        QuestId = entityId
                                    });
                                    break;
                                default:
                                    _loggingService.LogWarning($"Unknown entity type: {entityType}");
                                    break;
                            }
                        }
                    }
                }
                        
                // Process partial updates
                if (updates["partialUpdates"] != null)
                {
                    var partialUpdates = updates["partialUpdates"] as JObject;
                    if (partialUpdates != null)
                    {
                        foreach (var property in partialUpdates.Properties())
                        {
                            var entityId = property.Name;
                            var updateData = property.Value as JObject;
                                    
                            if (updateData == null)
                            {
                                _loggingService.LogWarning($"Invalid update data for entity {entityId}");
                                continue;
                            }
                                    
                            // Handle special case for player
                            if (entityId.ToLower() == "player")
                            {
                                _loggingService.LogInfo("Processing player update");
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
                                    
                            // Determine collection based on the entity type
                            string collection = "";
                            switch (entityType.ToLower())
                            {
                                case "npc":
                                    collection = "npcs";
                                    break;
                                case "location":
                                    collection = "locations";
                                    break;
                                case "quest":
                                    collection = "quests";
                                    break;
                                case "world":
                                    // Special case, no collection
                                    break;
                                case "player":
                                    // Special case, no collection
                                    break;
                                default:
                                    _loggingService.LogWarning($"Unknown entity type: {entityType} for entity {entityId}");
                                    continue;
                            }
                                    
                            // Only apply the update for the fields that were provided
                            await UpdateEntityAsync(userId, collection, entityId, updateData.ToString());
                        }
                    }
                }

                    
                // Do not wait for background tasks to complete
                _loggingService.LogInfo("DM updates processing complete");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error processing DM updates: {ex.Message}");
                throw;
            }
        }
        
        private async Task UpdateEntityAsync(string userId, string entityType, string entityId, string updateData)
        {
            var filePath = string.IsNullOrEmpty(entityType) 
                ? entityId 
                : $"{entityType}/{entityId}";
                
            await _storageService.ApplyPartialUpdateAsync(userId, filePath, updateData);
            _loggingService.LogInfo($"Updated {entityType} entity: {entityId}");
        }
        
        private async Task TriggerEntityCreationJob(PromptRequest request)
        {
            const int maxRetries = 3;
            int currentRetry = 0;
            
            while (currentRetry < maxRetries)
            {
                try
                {
                    _loggingService.LogInfo($"Triggering {request.PromptType} job for user {request.UserId} with input: {request.Context} (attempt {currentRetry + 1})");
                    string result = await _backgroundJobService.EnqueuePromptAsync(request);
                    _loggingService.LogInfo($"Completed {request.PromptType} job for user {request.UserId}. Result length: {result?.Length ?? 0}");
                    
                    // If we get here, the job was successful, so we can return
                    return;
                }
                catch (Exception ex)
                {
                    currentRetry++;
                    _loggingService.LogError($"Error triggering entity creation job (attempt {currentRetry}): {ex.Message}");
                    
                    if (currentRetry >= maxRetries)
                    {
                        _loggingService.LogError($"Max retries ({maxRetries}) reached for {request.PromptType} job. Giving up.");
                        // We don't rethrow here to avoid cascading failures
                    }
                    else
                    {
                        // Wait before retrying, with exponential backoff
                        int delayMs = 1000 * (int)Math.Pow(2, currentRetry - 1); // 1s, 2s, 4s...
                        _loggingService.LogInfo($"Retrying in {delayMs}ms...");
                        await Task.Delay(delayMs);
                    }
                }
            }
        }

        // New helper method to fire and forget entity creation jobs
        private void FireAndForgetEntityCreation(PromptRequest request)
        {
            Task.Run(async () => 
            {
                try 
                {
                    await TriggerEntityCreationJob(request);
                }
                catch (Exception ex) 
                {
                    _loggingService.LogError($"Error in fire-and-forget job for {request.PromptType} creation: {ex.Message}");
                }
            });
        }
    }
} 