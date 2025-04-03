using System.Threading.Tasks;
using AiGMBackEnd.Models;
using AiGMBackEnd.Services;
using Newtonsoft.Json.Linq;

namespace AiGMBackEnd.Services.Processors
{
    public class PlayerProcessor : IPlayerProcessor
    {
        private readonly StorageService _storageService;
        private readonly LoggingService _loggingService;

        public PlayerProcessor(
            StorageService storageService,
            LoggingService loggingService)
        {
            _storageService = storageService;
            _loggingService = loggingService;
        }

        public async Task ProcessAsync(JObject playerData, string userId)
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
                        Body = visualDesc["body"]?.ToString(),
                        VisibleClothing = visualDesc["visibleClothing"]?.ToString(),
                        Condition = visualDesc["condition"]?.ToString(),
                        ResemblingCelebrity = visualDesc["resemblingCelebrity"]?.ToString()
                    };
                }

                if (playerData["age"] != null)
                {
                    player.Money = playerData["age"].Value<int>();
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
    }
} 