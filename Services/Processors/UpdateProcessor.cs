using System.Threading.Tasks;
using AiGMBackEnd.Models;
using AiGMBackEnd.Services;
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
                _loggingService.LogInfo("Processing DM updates");
                
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
                                    
                            if (string.IsNullOrEmpty(entityId))
                            {
                                _loggingService.LogWarning("Skipping entity with missing ID");
                                continue;
                            }
                                    
                            _loggingService.LogInfo($"Processing creation of {entityType} entity: {entityId}");
                                    
                            // First, save the basic entity data immediately
                            switch (entityType)
                            {
                                case "npc":
                                    if (entity is JObject npcObj)
                                    {
                                        await _npcProcessor.ProcessAsync(npcObj, userId);
                                    }
                                    // Fire and forget - do not wait for job completion
                                    var npcName = entity["name"]?.ToString() ?? "New NPC";
                                    _loggingService.LogInfo($"Will trigger separate job to create NPC: {npcName}");
                                    FireAndForgetEntityCreation(PromptType.CreateNPC, userId, $"Create {npcName}");
                                    break;
                                case "location":
                                    if (entity is JObject locationObj)
                                    {
                                        await _locationProcessor.ProcessAsync(locationObj, userId);
                                    }
                                    // Fire and forget - do not wait for job completion
                                    var locationName = entity["name"]?.ToString() ?? "New Location";
                                    _loggingService.LogInfo($"Will trigger separate job to create location: {locationName}");
                                    FireAndForgetEntityCreation(PromptType.CreateLocation, userId, $"Create {locationName}");
                                    break;
                                case "quest":
                                    if (entity is JObject questObj)
                                    {
                                        await _questProcessor.ProcessAsync(questObj, userId);
                                    }
                                    // Fire and forget - do not wait for job completion
                                    var questTitle = entity["title"]?.ToString() ?? "New Quest";
                                    _loggingService.LogInfo($"Will trigger separate job to create quest: {questTitle}");
                                    FireAndForgetEntityCreation(PromptType.CreateQuest, userId, $"Create {questTitle}");
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
        
        private async Task TriggerEntityCreationJob(PromptType promptType, string userId, string userInput)
        {
            const int maxRetries = 3;
            int currentRetry = 0;
            
            while (currentRetry < maxRetries)
            {
                try
                {
                    var job = new PromptJob
                    {
                        UserId = userId,
                        UserInput = userInput,
                        PromptType = promptType
                    };
                    
                    _loggingService.LogInfo($"Triggering {promptType} job for user {userId} with input: {userInput} (attempt {currentRetry + 1})");
                    string result = await _backgroundJobService.EnqueuePromptAsync(job);
                    _loggingService.LogInfo($"Completed {promptType} job for user {userId}. Result length: {result?.Length ?? 0}");
                    
                    // If we get here, the job was successful, so we can return
                    return;
                }
                catch (Exception ex)
                {
                    currentRetry++;
                    _loggingService.LogError($"Error triggering entity creation job (attempt {currentRetry}): {ex.Message}");
                    
                    if (currentRetry >= maxRetries)
                    {
                        _loggingService.LogError($"Max retries ({maxRetries}) reached for {promptType} job. Giving up.");
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
        private void FireAndForgetEntityCreation(PromptType promptType, string userId, string userInput)
        {
            Task.Run(async () => 
            {
                try 
                {
                    await TriggerEntityCreationJob(promptType, userId, userInput);
                }
                catch (Exception ex) 
                {
                    _loggingService.LogError($"Error in fire-and-forget job for {promptType} creation: {ex.Message}");
                }
            });
        }
    }
} 