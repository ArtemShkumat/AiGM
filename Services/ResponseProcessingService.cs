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
                else
                {
                    _loggingService.LogInfo($"No hiddenJson in response for user {userId}");
                }

                // Add DM's message to conversation log for DM and NPC responses
                if (promptType == PromptType.DM || promptType == PromptType.NPC)
                {
                    await _storageService.AddDmMessageAsync(userId, userFacingText);
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

        private async Task ProcessHiddenJsonAsync(string jsonContent, PromptType promptType, string userId)
        {
            try
            {
                // Deserialize JSON content
                JObject jsonObject;
                try
                {
                    jsonObject = JObject.Parse(jsonContent);
                }
                catch (JsonException)
                {
                    // Try to fix common JSON issues like extra newlines
                    jsonContent = jsonContent.Trim();
                    jsonObject = JObject.Parse(jsonContent);
                }

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
                                        await CreateNewEntityAsync(entity, userId, "npcs", entityId);
                                        // Fire and forget - do not wait for job completion
                                        var npcName = entity["name"]?.ToString() ?? "New NPC";
                                        _loggingService.LogInfo($"Will trigger separate job to create NPC: {npcName}");
                                        FireAndForgetEntityCreation(PromptType.CreateNPC, userId, $"Create {npcName}");
                                        break;
                                    case "location":
                                        await CreateNewEntityAsync(entity, userId, "locations", entityId);
                                        // Fire and forget - do not wait for job completion
                                        var locationName = entity["name"]?.ToString() ?? "New Location";
                                        _loggingService.LogInfo($"Will trigger separate job to create location: {locationName}");
                                        FireAndForgetEntityCreation(PromptType.CreateLocation, userId, $"Create {locationName}");
                                        break;
                                    case "quest":
                                        await CreateNewEntityAsync(entity, userId, "quests", entityId);
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
                    if (dmUpdates["partialUpdates"] != null)
                    {
                        var partialUpdates = dmUpdates["partialUpdates"] as JObject;
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
                                var updateData = property.Value as JObject;
                                
                                if (updateData == null)
                                {
                                    _loggingService.LogWarning($"Invalid update data for entity {entityId}");
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
                                
                                // For NPC updates, we only handle NPCs in this method
                                if (entityType.ToLower() == "npc")
                                {
                                    await UpdateEntityAsync(userId, "npcs", entityId, updateData.ToString());
                                }
                                else
                                {
                                    _loggingService.LogWarning($"Unexpected entity type {entityType} in NPC updates for entity {entityId}");
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
                
                // Create a new Location object based on our model class
                var location = new Models.Location
                {
                    Id = locationId,
                    Name = locationData["name"]?.ToString() ?? "Unknown Location",
                    Type = locationData["type"]?.ToString(),
                    Description = locationData["description"]?.ToString(),
                    DiscoveredByPlayer = locationData["discoveredByPlayer"]?.Value<bool>() ?? false
                };
                
                // Handle Connected Locations
                if (locationData["connectedLocations"] is JArray connectedLocations)
                {
                    foreach (var conn in connectedLocations)
                    {
                        if (conn is JObject connObj)
                        {
                            location.ConnectedLocations.Add(new Models.ConnectedLocation
                            {
                                Id = connObj["id"]?.ToString(),
                                Description = connObj["description"]?.ToString()
                            });
                        }
                    }
                }
                
                // Handle Parent Location
                if (locationData["parentLocation"] is JObject parentLoc)
                {
                    location.ParentLocation = new Models.ParentLocation
                    {
                        Id = parentLoc["id"]?.ToString(),
                        Description = parentLoc["description"]?.ToString()
                    };
                }
                
                // Handle Sub Locations
                if (locationData["subLocations"] is JArray subLocations)
                {
                    foreach (var sub in subLocations)
                    {
                        if (sub is JObject subObj)
                        {
                            location.SubLocations.Add(new Models.SubLocation
                            {
                                Id = subObj["id"]?.ToString(),
                                Description = subObj["description"]?.ToString()
                            });
                        }
                    }
                }
                
                // Handle NPCs
                if (locationData["npcs"] is JArray npcs)
                {
                    foreach (var npc in npcs)
                    {
                        var npcStr = npc.ToString();
                        if (!string.IsNullOrEmpty(npcStr))
                        {
                            location.Npcs.Add(npcStr);
                        }
                    }
                }
                
                // Handle Points of Interest
                if (locationData["pointsOfInterest"] is JArray pois)
                {
                    foreach (var poi in pois)
                    {
                        if (poi is JObject poiObj)
                        {
                            location.PointsOfInterest.Add(new Models.PointOfInterest
                            {
                                Name = poiObj["name"]?.ToString(),
                                Description = poiObj["description"]?.ToString()
                            });
                        }
                    }
                }
                
                // Handle Quest IDs
                if (locationData["questIds"] is JArray questIds)
                {
                    foreach (var quest in questIds)
                    {
                        var questStr = quest.ToString();
                        if (!string.IsNullOrEmpty(questStr))
                        {
                            location.QuestIds.Add(questStr);
                        }
                    }
                }
                
                // Handle Items
                if (locationData["items"] is JArray items)
                {
                    foreach (var item in items)
                    {
                        var itemStr = item.ToString();
                        if (!string.IsNullOrEmpty(itemStr))
                        {
                            location.Items.Add(itemStr);
                        }
                    }
                }
                
                // Handle History Log
                if (locationData["historyLog"] is JArray historyLog)
                {
                    foreach (var log in historyLog)
                    {
                        if (log is JObject logObj)
                        {
                            location.HistoryLog.Add(new Models.HistoryLogEntry
                            {
                                Timestamp = logObj["timestamp"]?.ToString(),
                                Event = logObj["event"]?.ToString(),
                                NpcId = logObj["npcId"]?.ToString(),
                                Description = logObj["description"]?.ToString()
                            });
                        }
                    }
                }
                
                // Save the location data
                await _storageService.SaveAsync(userId, $"locations/{locationId}", location);
                
                // Check if there are associated NPCs to create
                if (locationData["npcs"] != null)
                {
                    var npcsArray = locationData["npcs"] as JArray;
                    if (npcsArray != null && npcsArray.Count > 0)
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
                
                // Create a new Quest object based on our model class
                var quest = new Models.Quest
                {
                    Id = questId,
                    Title = questData["title"]?.ToString() ?? "Unknown Quest",
                    CurrentProgress = questData["currentProgress"]?.ToString(),
                    QuestDescription = questData["questDescription"]?.ToString(),
                    Notes = questData["notes"]?.ToString()
                };
                
                // Handle Achievement Conditions
                if (questData["achievementConditions"] is JArray achievementConditions)
                {
                    foreach (var condition in achievementConditions)
                    {
                        var conditionStr = condition.ToString();
                        if (!string.IsNullOrEmpty(conditionStr))
                        {
                            quest.AchievementConditions.Add(conditionStr);
                        }
                    }
                }
                
                // Handle Fail Conditions
                if (questData["failConditions"] is JArray failConditions)
                {
                    foreach (var condition in failConditions)
                    {
                        var conditionStr = condition.ToString();
                        if (!string.IsNullOrEmpty(conditionStr))
                        {
                            quest.FailConditions.Add(conditionStr);
                        }
                    }
                }
                
                // Handle Involved Locations
                if (questData["involvedLocations"] is JArray involvedLocations)
                {
                    foreach (var location in involvedLocations)
                    {
                        var locationStr = location.ToString();
                        if (!string.IsNullOrEmpty(locationStr))
                        {
                            quest.InvolvedLocations.Add(locationStr);
                        }
                    }
                }
                
                // Handle Involved NPCs
                if (questData["involvedNpcs"] is JArray involvedNpcs)
                {
                    foreach (var npc in involvedNpcs)
                    {
                        var npcStr = npc.ToString();
                        if (!string.IsNullOrEmpty(npcStr))
                        {
                            quest.InvolvedNpcs.Add(npcStr);
                        }
                    }
                }
                
                // Handle Quest Log
                if (questData["questLog"] is JArray questLog)
                {
                    foreach (var log in questLog)
                    {
                        if (log is JObject logObj)
                        {
                            quest.QuestLog.Add(new Models.QuestLogEntry
                            {
                                Timestamp = logObj["timestamp"]?.ToString(),
                                Event = logObj["event"]?.ToString(),
                                Description = logObj["description"]?.ToString()
                            });
                        }
                    }
                }
                
                // Save the quest data
                await _storageService.SaveAsync(userId, $"quests/{questId}", quest);
                
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
                
                // Create a new NPC object based on our model class
                var npc = new Models.Npc
                {
                    Id = npcId,
                    Name = npcData["name"]?.ToString() ?? "Unknown NPC",
                    CurrentLocationId = npcData["currentLocationId"]?.ToString(),
                    KnownToPlayer = npcData["discoveredByPlayer"]?.Value<bool>() ?? false,
                    KnowsPlayer = npcData["knowsPlayer"]?.Value<bool>() ?? false,
                    VisibleToPlayer = npcData["visibleToPlayer"]?.Value<bool>() ?? true,
                    Backstory = npcData["backstory"]?.ToString(),
                    DispositionTowardsPlayer = npcData["dispositionTowardsPlayer"]?.ToString()
                };
                
                // Handle Visual Description
                if (npcData["visualDescription"] is JObject visualDesc)
                {
                    npc.VisualDescription = new Models.VisualDescription
                    {
                        Gender = visualDesc["gender"]?.ToString(),
                        Body = visualDesc["bodyType"]?.ToString(),
                        VisibleClothing = visualDesc["visibleClothing"]?.ToString(),
                        Condition = visualDesc["condition"]?.ToString()
                    };
                }
                
                // Handle Personality
                if (npcData["personality"] is JObject personality)
                {
                    npc.Personality = new Models.Personality
                    {
                        Temperament = personality["temperament"]?.ToString(),
                        Quirks = personality["traits"]?.ToString()
                    };
                }
                
                // Handle Known Entities
                if (npcData["knownEntities"] is JObject knownEntities)
                {
                    if (knownEntities["npcsKnown"] is JArray npcsKnown)
                    {
                        foreach (var knownNpc in npcsKnown)
                        {
                            if (knownNpc is JObject knownNpcObj)
                            {
                                npc.KnownEntities.NpcsKnown.Add(new Models.NpcsKnownDetails
                                {
                                    Name = knownNpcObj["name"]?.ToString(),
                                    LevelOfFamiliarity = knownNpcObj["levelOfFamiliarity"]?.ToString(),
                                    Disposition = knownNpcObj["disposition"]?.ToString()
                                });
                            }
                        }
                    }
                    
                    if (knownEntities["locationsKnown"] is JArray locationsKnown)
                    {
                        foreach (var knownLoc in locationsKnown)
                        {
                            var knownLocStr = knownLoc.ToString();
                            if (!string.IsNullOrEmpty(knownLocStr))
                            {
                                npc.KnownEntities.LocationsKnown.Add(knownLocStr);
                            }
                        }
                    }
                }
                
                // Handle Quest Involvement
                if (npcData["questInvolvement"] is JArray questInvolvement)
                {
                    foreach (var quest in questInvolvement)
                    {
                        var questStr = quest.ToString();
                        if (!string.IsNullOrEmpty(questStr))
                        {
                            npc.QuestInvolvement.Add(questStr);
                        }
                    }
                }
                
                // Handle Inventory
                if (npcData["inventory"] is JArray inventory)
                {
                    foreach (var item in inventory)
                    {
                        if (item is JObject itemObj)
                        {
                            npc.Inventory.Add(new Models.InventoryItem
                            {
                                Name = itemObj["name"]?.ToString(),
                                Description = itemObj["description"]?.ToString(),
                                Quantity = itemObj["quantity"]?.Value<int>() ?? 1
                            });
                        }
                    }
                }
                
                // Handle Conversation Log
                if (npcData["conversationLog"] is JArray conversationLog)
                {
                    foreach (var log in conversationLog)
                    {
                        if (log is JObject logObj)
                        {
                            var entry = new Dictionary<string, string>();
                            foreach (var prop in logObj.Properties())
                            {
                                entry[prop.Name] = prop.Value.ToString();
                            }
                            npc.ConversationLog.Add(entry);
                        }
                    }
                }
                
                // Save the NPC data
                await _storageService.SaveAsync(userId, $"npcs/{npcId}", npc);
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error processing NPC creation: {ex.Message}");
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
                
                // Create a new Player object
                var player = new Models.Player
                {
                    Id = playerId,
                    Name = playerData["name"]?.ToString() ?? "Unknown",
                    CurrentLocationId = playerData["currentLocationId"]?.ToString(),
                    Backstory = playerData["backstory"]?.ToString()
                };
                
                // Handle VisualDescription
                if (playerData["visualDescription"] is JObject visualDesc)
                {
                    player.VisualDescription = new Models.VisualDescription
                    {
                        Gender = visualDesc["gender"]?.ToString(),
                        Body = visualDesc["bodyType"]?.ToString(),
                        VisibleClothing = visualDesc["visibleClothing"]?.ToString(),
                        Condition = visualDesc["condition"]?.ToString(),
                        ResemblingCelebrity = visualDesc["resemblingCelebrity"]?.ToString()
                    };
                }
                
                // Handle inventory
                if (playerData["inventory"] is JArray inventory)
                {
                    foreach (var item in inventory)
                    {
                        if (item is JObject itemObj)
                        {
                            player.Inventory.Add(new Models.InventoryItem
                            {
                                Name = itemObj["name"]?.ToString(),
                                Description = itemObj["description"]?.ToString(),
                                Quantity = itemObj["quantity"]?.Value<int>() ?? 1
                            });
                        }
                    }
                }
                
                // Handle money
                if (playerData["money"] != null)
                {
                    player.Money = playerData["money"].Value<int>();
                }
                
                // Handle status effects
                if (playerData["statusEffects"] is JArray statusEffects)
                {
                    foreach (var effect in statusEffects)
                    {
                        var effectStr = effect.ToString();
                        if (!string.IsNullOrEmpty(effectStr))
                        {
                            player.StatusEffects.Add(effectStr);
                        }
                    }
                }
                
                // Handle RPG elements - this is a special case as it's a dictionary
                if (playerData["rpgElements"] is JObject rpgElements)
                {
                    foreach (var prop in rpgElements.Properties())
                    {
                        // Handle different value types
                        if (prop.Value is JObject)
                        {
                            // Convert JObject to Dictionary
                            var dict = prop.Value.ToObject<Dictionary<string, object>>();
                            player.RpgElements[prop.Name] = dict;
                        }
                        else if (prop.Value is JArray)
                        {
                            // Convert JArray to List
                            var list = prop.Value.ToObject<List<object>>();
                            player.RpgElements[prop.Name] = list;
                        }
                        else
                        {
                            // Simple properties
                            player.RpgElements[prop.Name] = prop.Value.ToObject<object>();
                        }
                    }
                }
                
                // Handle active quests
                if (playerData["activeQuests"] is JArray activeQuests)
                {
                    foreach (var quest in activeQuests)
                    {
                        var questStr = quest.ToString();
                        if (!string.IsNullOrEmpty(questStr))
                        {
                            player.ActiveQuests.Add(questStr);
                        }
                    }
                }
                
                // Handle completed quests
                if (playerData["completedQuests"] is JArray completedQuests)
                {
                    if (player.RpgElements.ContainsKey("completedQuests"))
                    {
                        // Update existing
                        var existing = player.RpgElements["completedQuests"] as List<object>;
                        if (existing != null)
                        {
                            foreach (var quest in completedQuests)
                            {
                                var questStr = quest.ToString();
                                if (!string.IsNullOrEmpty(questStr) && !existing.Contains(questStr))
                                {
                                    existing.Add(questStr);
                                }
                            }
                        }
                    }
                    else
                    {
                        // Create new
                        player.RpgElements["completedQuests"] = completedQuests.ToObject<List<object>>();
                    }
                }               
                
                
                // Save the player data
                await _storageService.SaveAsync(userId, "player", player);
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
            try
            {
                switch (entityType)
                {
                    case "npcs":
                        if (entity is JObject npcObj)
                        {
                            await ProcessNPCCreationAsync(npcObj, userId);
                        }
                        break;
                    case "locations":
                        if (entity is JObject locationObj)
                        {
                            await ProcessLocationCreationAsync(locationObj, userId);
                        }
                        break;
                    case "quests":
                        if (entity is JObject questObj)
                        {
                            await ProcessQuestCreationAsync(questObj, userId);
                        }
                        break;
                    default:
                        _loggingService.LogWarning($"Unknown entity type: {entityType}");
                        break;
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error creating entity {entityType}/{entityId}: {ex.Message}");
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

        private (string userFacingText, string hiddenJson) ExtractHiddenJson(string llmResponse)
        {
            // Pattern to extract content between <donotshow/> tags
            llmResponse = Regex.Replace(llmResponse, @"^```json\s*|\s*```$", string.Empty, RegexOptions.Multiline).Trim();
            var regex = new Regex(@"^(.*?)<donotshow/>(.*)$", RegexOptions.Singleline);
            var match = regex.Match(llmResponse);

            if (match.Success)
            {
                var userFacingText = match.Groups[1].Value.Trim();
                var jsonContent = match.Groups[2].Value.Trim();

                int jsonStartIndex = jsonContent.IndexOf('{');
                if (jsonStartIndex == -1)
                    jsonStartIndex = jsonContent.IndexOf('[');

                if (jsonStartIndex >= 0)
                {
                    var jsonCandidate = jsonContent.Substring(jsonStartIndex).Trim();
                    // Try to find the end of the JSON
                    try
                    {
                        // Validate that this is valid JSON
                        JToken.Parse(jsonCandidate);
                        return (userFacingText, jsonCandidate);
                    }
                    catch (JsonException ex)
                    {
                        _loggingService.LogWarning($"Invalid JSON found in hidden content: {ex.Message}");
                        // Return what we have anyway and let the processor handle it
                        return (userFacingText, jsonCandidate);
                    }
                }

                return (userFacingText, jsonContent);
            }

            // No hidden content found
            return (llmResponse, string.Empty);
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
