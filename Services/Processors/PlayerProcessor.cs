using System.Threading.Tasks;
using AiGMBackEnd.Models;
using AiGMBackEnd.Services;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

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
                    player.Age = playerData["age"].Value<int>();
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
                
                // Handle currencies (if present in new format)
                if (playerData["currencies"] is JArray currencies)
                {
                    foreach (var currency in currencies)
                    {
                        if (currency is JObject currencyObj)
                        {
                            player.Currencies.Add(new Models.Currency
                            {
                                Name = currencyObj["name"]?.ToString() ?? "Unknown",
                                Amount = currencyObj["amount"]?.Value<int>() ?? 0
                            });
                        }
                    }
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
                
                // Handle RPG tags
                if (playerData["rpgTags"] is JArray rpgTags)
                {
                    foreach (var tag in rpgTags)
                    {
                        if (tag is JObject tagObj)
                        {
                            player.RpgTags.Add(new Models.RpgTag
                            {
                                Name = tagObj["name"]?.ToString(),
                                Description = tagObj["description"]?.ToString()
                            });
                        }
                    }
                }
                // Handle completed quests - moved outside the rpgElements section
                if (playerData["completedQuests"] is JArray completedQuests)
                {
                    // Handle completed quests in a different way since we're no longer using rpgElements
                    List<string> completedQuestsList = new List<string>();
                    
                    foreach (var quest in completedQuests)
                    {
                        var questStr = quest.ToString();
                        if (!string.IsNullOrEmpty(questStr))
                        {
                            completedQuestsList.Add(questStr);
                        }
                    }
                    
                    // Store as a separate property if needed, or update this as necessary
                    // For now, let's add a special tag
                    if (completedQuestsList.Count > 0)
                    {
                        player.RpgTags.Add(new Models.RpgTag
                        {
                            Name = "Completed Quests",
                            Description = string.Join(", ", completedQuestsList)
                        });
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