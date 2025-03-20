using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AiGMBackEnd.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AiGMBackEnd.Services
{
    public class ResponseProcessingService
    {
        private readonly StorageService _storageService;
        private readonly LoggingService _loggingService;
        private readonly BackgroundJobService _backgroundJobService;

        public ResponseProcessingService(
            StorageService storageService,
            LoggingService loggingService,
            BackgroundJobService backgroundJobService)
        {
            _storageService = storageService;
            _loggingService = loggingService;
            _backgroundJobService = backgroundJobService;
        }

        public async Task<ProcessedResult> HandleResponseAsync(string llmResponse, PromptType promptType, string userId)
        {
            try
            {
                _loggingService.LogInfo($"Processing {promptType} response for user {userId}");
                
                // Extract hidden JSON and user-facing text
                var (userFacingText, hiddenJson) = ExtractHiddenJson(llmResponse);

                // Process any state updates or entity creation based on the hidden JSON
                if (!string.IsNullOrEmpty(hiddenJson))
                {
                    await ProcessHiddenJsonAsync(hiddenJson, promptType, userId);
                }

                return new ProcessedResult
                {
                    UserFacingText = userFacingText,
                    Success = true,
                    ErrorMessage = string.Empty
                };
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error processing response: {ex.Message}");
                return new ProcessedResult
                {
                    UserFacingText = "Something went wrong when processing the response.",
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private async Task ProcessHiddenJsonAsync(string hiddenJson, PromptType promptType, string userId)
        {
            try
            {
                // Parse JSON
                var jsonObject = JObject.Parse(hiddenJson);

                switch (promptType)
                {
                    case PromptType.DM:
                        await ProcessDMUpdatesAsync(jsonObject, userId);
                        break;
                    case PromptType.NPC:
                        await ProcessNPCUpdatesAsync(jsonObject, userId);
                        break;
                    case PromptType.CreateQuest:
                    case PromptType.CreateQuestJson:
                        await ProcessQuestCreationAsync(jsonObject, userId);
                        break;
                    case PromptType.CreateNPC:
                    case PromptType.CreateNPCJson:
                        await ProcessNPCCreationAsync(jsonObject, userId);
                        break;
                    case PromptType.CreateLocation:
                    case PromptType.CreateLocationJson:
                        await ProcessLocationCreationAsync(jsonObject, userId);
                        break;
                    case PromptType.CreatePlayerJson:
                        await ProcessPlayerCreationAsync(jsonObject, userId);
                        break;
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error processing hidden JSON: {ex.Message}");
                throw;
            }
        }

        private async Task ProcessDMUpdatesAsync(JObject updates, string userId)
        {
            try
            {
                _loggingService.LogInfo("Processing DM updates");
                
                // Check if there are dmUpdates
                if (updates["dmUpdates"] != null)
                {
                    var dmUpdates = updates["dmUpdates"];
                    
                    // Process new entities
                    if (dmUpdates["newEntities"] != null)
                    {
                        var newEntities = dmUpdates["newEntities"] as JArray;
                        if (newEntities != null)
                        {
                            foreach (var entity in newEntities)
                            {
                                var entityType = entity["type"]?.ToString()?.ToLower();
                                var entityId = entity["id"]?.ToString();
                                
                                if (string.IsNullOrEmpty(entityType) || string.IsNullOrEmpty(entityId))
                                {
                                    _loggingService.LogWarning($"Skipping entity with missing type or id: {entity}");
                                    continue;
                                }
                                
                                switch (entityType)
                                {
                                    case "npc":
                                        await CreateNewEntityAsync(entity, userId, "npcs", entityId);
                                        break;
                                    case "location":
                                        await CreateNewEntityAsync(entity, userId, "locations", entityId);
                                        // Trigger a background job to fully create the location
                                        var locationName = entity["name"]?.ToString() ?? "New Location";
                                        await TriggerEntityCreationJob(PromptType.CreateLocation, userId, $"Create {locationName}");
                                        break;
                                    case "quest":
                                        await CreateNewEntityAsync(entity, userId, "quests", entityId);
                                        // Trigger a background job to fully create the quest
                                        var questTitle = entity["title"]?.ToString() ?? "New Quest";
                                        await TriggerEntityCreationJob(PromptType.CreateQuest, userId, $"Create {questTitle}");
                                        break;
                                    default:
                                        _loggingService.LogWarning($"Unknown entity type: {entityType}");
                                        break;
                                }
                            }
                        }
                    }
                    
                    // Process partial updates
                    if (dmUpdates["partialUpdates"] != null)
                    {
                        var partialUpdates = dmUpdates["partialUpdates"] as JObject;
                        if (partialUpdates != null)
                        {
                            foreach (var property in partialUpdates.Properties())
                            {
                                var entityId = property.Name;
                                var updateData = property.Value.ToString();
                                
                                // Determine entity type from ID prefix
                                if (entityId.StartsWith("npc_"))
                                {
                                    await UpdateEntityAsync(userId, "npcs", entityId, updateData);
                                }
                                else if (entityId.StartsWith("loc_"))
                                {
                                    await UpdateEntityAsync(userId, "locations", entityId, updateData);
                                }
                                else if (entityId.StartsWith("quest_"))
                                {
                                    await UpdateEntityAsync(userId, "quests", entityId, updateData);
                                }
                                else if (entityId == "world")
                                {
                                    await UpdateEntityAsync(userId, "", "world", updateData);
                                }
                                else if (entityId == "player")
                                {
                                    await UpdateEntityAsync(userId, "", "player", updateData);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error in ProcessDMUpdatesAsync: {ex.Message}");
                throw;
            }
        }

        private async Task ProcessNPCUpdatesAsync(JObject updates, string userId)
        {
            try
            {
                _loggingService.LogInfo("Processing NPC updates");
                
                // Check if there are npcUpdates
                if (updates["npcUpdates"] != null)
                {
                    var npcUpdates = updates["npcUpdates"];
                    
                    // Process new entities
                    if (npcUpdates["newEntities"] != null)
                    {
                        var newEntities = npcUpdates["newEntities"] as JArray;
                        if (newEntities != null)
                        {
                            foreach (var entity in newEntities)
                            {
                                var entityType = entity["type"]?.ToString()?.ToLower();
                                var entityId = entity["id"]?.ToString();
                                
                                if (string.IsNullOrEmpty(entityType) || string.IsNullOrEmpty(entityId))
                                {
                                    _loggingService.LogWarning($"Skipping entity with missing type or id: {entity}");
                                    continue;
                                }
                                
                                switch (entityType)
                                {
                                    case "npc":
                                        await CreateNewEntityAsync(entity, userId, "npcs", entityId);
                                        break;
                                    case "quest":
                                        await CreateNewEntityAsync(entity, userId, "quests", entityId);
                                        var questTitle = entity["title"]?.ToString() ?? "New Quest";
                                        await TriggerEntityCreationJob(PromptType.CreateQuest, userId, $"Create {questTitle}");
                                        break;
                                }
                            }
                        }
                    }
                    
                    // Process partial updates
                    if (npcUpdates["partialUpdates"] != null)
                    {
                        var partialUpdates = npcUpdates["partialUpdates"] as JObject;
                        if (partialUpdates != null)
                        {
                            foreach (var property in partialUpdates.Properties())
                            {
                                var entityId = property.Name;
                                var updateData = property.Value.ToString();
                                
                                // Handle NPC updates
                                if (entityId.StartsWith("npc_"))
                                {
                                    await UpdateEntityAsync(userId, "npcs", entityId, updateData);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error in ProcessNPCUpdatesAsync: {ex.Message}");
                throw;
            }
        }

        private async Task ProcessQuestCreationAsync(JObject questData, string userId)
        {
            try
            {
                _loggingService.LogInfo("Processing quest creation");
                
                // Extract quest details
                var questId = questData["id"]?.ToString();
                
                if (string.IsNullOrEmpty(questId))
                {
                    _loggingService.LogError("Quest ID is missing");
                    return;
                }
                
                // Save the quest data - pass the JObject directly
                await _storageService.SaveAsync(userId, $"quests/{questId}", questData);
                
                // Check if there are associated entities to create
                if (questData["involvedLocations"] != null || questData["involvedNpcs"] != null)
                {
                    // TODO: Trigger jobs to create missing locations and NPCs if needed
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error processing quest creation: {ex.Message}");
                throw;
            }
        }

        private async Task ProcessNPCCreationAsync(JObject npcData, string userId)
        {
            try
            {
                _loggingService.LogInfo("Processing NPC creation");
                
                // Extract NPC details
                var npcId = npcData["id"]?.ToString();
                
                if (string.IsNullOrEmpty(npcId))
                {
                    _loggingService.LogError("NPC ID is missing");
                    return;
                }
                
                // Save the NPC data - pass the JObject directly
                await _storageService.SaveAsync(userId, $"npcs/{npcId}", npcData);
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error processing NPC creation: {ex.Message}");
                throw;
            }
        }

        private async Task ProcessLocationCreationAsync(JObject locationData, string userId)
        {
            try
            {
                _loggingService.LogInfo("Processing location creation");
                
                // Extract location details
                var locationId = locationData["id"]?.ToString();
                
                if (string.IsNullOrEmpty(locationId))
                {
                    _loggingService.LogError("Location ID is missing");
                    return;
                }
                
                // Save the location data - pass the JObject directly
                await _storageService.SaveAsync(userId, $"locations/{locationId}", locationData);
                
                // Check if there are associated NPCs to create
                if (locationData["npcs"] != null)
                {
                    var npcs = locationData["npcs"] as JArray;
                    if (npcs != null && npcs.Count > 0)
                    {
                        // TODO: Trigger jobs to create missing NPCs if needed
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error processing location creation: {ex.Message}");
                throw;
            }
        }
        
        private async Task ProcessPlayerCreationAsync(JObject playerData, string userId)
        {
            try
            {
                _loggingService.LogInfo("Processing player creation");
                
                // Extract player details
                var playerId = playerData["id"]?.ToString();
                
                if (string.IsNullOrEmpty(playerId))
                {
                    _loggingService.LogError("Player ID is missing");
                    return;
                }
                
                // Save the player data - pass the JObject directly instead of converting to string
                await _storageService.SaveAsync(userId, "player", playerData);
                _loggingService.LogInfo($"Created/Updated player: {playerId}");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error processing player creation: {ex.Message}");
                throw;
            }
        }
        
        private async Task CreateNewEntityAsync(JToken entity, string userId, string entityType, string entityId)
        {
            // Save the new entity to storage - pass the JToken directly
            await _storageService.SaveAsync(userId, $"{entityType}/{entityId}", entity);
            _loggingService.LogInfo($"Created new {entityType} entity: {entityId}");
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
            try
            {
                var job = new PromptJob
                {
                    UserId = userId,
                    UserInput = userInput,
                    PromptType = promptType
                };
                
                await _backgroundJobService.EnqueuePromptAsync(job);
                _loggingService.LogInfo($"Triggered {promptType} job for user {userId}");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error triggering entity creation job: {ex.Message}");
                // We don't rethrow here to avoid cascading failures
            }
        }

        private (string userFacingText, string hiddenJson) ExtractHiddenJson(string llmResponse)
        {
            // Pattern to extract content between <donotshow> tags
            var regex = new Regex(@"(.*?)<donotshow>(.*?)</donotshow>(.*?)", RegexOptions.Singleline);
            var match = regex.Match(llmResponse);

            if (match.Success)
            {
                var beforeTag = match.Groups[1].Value.Trim();
                var jsonContent = match.Groups[2].Value.Trim();
                var afterTag = match.Groups[3].Value.Trim();

                // Combine text before and after the tags
                var userFacingText = (beforeTag + " " + afterTag).Trim();
                
                return (userFacingText, jsonContent);
            }

            // No hidden content found
            return (llmResponse, string.Empty);
        }
    }
}
