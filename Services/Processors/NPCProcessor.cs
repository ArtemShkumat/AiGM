using System.Threading.Tasks;
using AiGMBackEnd.Models;
using AiGMBackEnd.Services;
using Newtonsoft.Json.Linq;

namespace AiGMBackEnd.Services.Processors
{
    public class NPCProcessor : IEntityProcessor
    {
        private readonly StorageService _storageService;
        private readonly LoggingService _loggingService;

        public NPCProcessor(
            StorageService storageService,
            LoggingService loggingService)
        {
            _storageService = storageService;
            _loggingService = loggingService;
        }

        public async Task ProcessAsync(JObject npcData, string userId)
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
                        Body = visualDesc["body"]?.ToString(),
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
    }
} 