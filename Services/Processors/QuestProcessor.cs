using System.Threading.Tasks;
using AiGMBackEnd.Models;
using AiGMBackEnd.Services;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System;

namespace AiGMBackEnd.Services.Processors
{
    public class QuestProcessor : IEntityProcessor
    {
        private readonly StorageService _storageService;
        private readonly LoggingService _loggingService;

        public QuestProcessor(
            StorageService storageService,
            LoggingService loggingService)
        {
            _storageService = storageService;
            _loggingService = loggingService;
        }

        public async Task ProcessAsync(JObject questData, string userId)
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
                    CoreObjective = questData["coreObjective"]?.ToString(),
                    Overview = questData["overview"]?.ToString(),
                    Rewards = new List<string>()
                };
                
                // Handle NPCs involved
                if (questData["npcs"] is JArray npcsArray)
                {
                    foreach (var npcData in npcsArray)
                    {
                        if (npcData is JObject npcObj)
                        {
                            var questNpc = new QuestNpc
                            {
                                Id = npcObj["id"]?.ToString(),
                                Name = npcObj["name"]?.ToString(),
                                Role = npcObj["role"]?.ToString(),
                                Motivation = npcObj["motivation"]?.ToString(),
                                Fears = npcObj["fears"]?.ToString(),
                                Secrets = npcObj["secrets"]?.ToString()
                            };
                            quest.Npcs.Add(questNpc);
                        }
                    }
                }
                
                // Handle Rumors and Leads
                if (questData["rumorsAndLeads"] is JArray rumorsArray)
                {
                    foreach (var rumorData in rumorsArray)
                    {
                        if (rumorData is JObject rumorObj)
                        {
                            var rumor = new RumorAndLead
                            {
                                Rumor = rumorObj["rumor"]?.ToString(),
                                SourceNPC = rumorObj["sourceNPC"]?.ToString(),
                                SourceLocation = rumorObj["sourceLocation"]?.ToString()
                            };
                            quest.RumorsAndLeads.Add(rumor);
                        }
                    }
                }
                
                // Handle Locations Involved
                if (questData["locationsInvolved"] is JArray locationsArray)
                {
                    foreach (var locationData in locationsArray)
                    {
                        if (locationData is JObject locationObj)
                        {
                            var location = new QuestLocation
                            {
                                Id = locationObj["id"]?.ToString(),
                                Name = locationObj["name"]?.ToString(),
                                Type = locationObj["type"]?.ToString()
                            };
                            quest.LocationsInvolved.Add(location);
                        }
                    }
                }
                
                // Handle Opposing Forces
                if (questData["opposingForces"] is JArray forcesArray)
                {
                    foreach (var forceData in forcesArray)
                    {
                        if (forceData is JObject forceObj)
                        {
                            var force = new OpposingForce
                            {
                                Name = forceObj["name"]?.ToString(),
                                Role = forceObj["role"]?.ToString(),
                                Motivation = forceObj["motivation"]?.ToString(),
                                Description = forceObj["description"]?.ToString()
                            };
                            quest.OpposingForces.Add(force);
                        }
                    }
                }
                
                // Handle Challenges
                if (questData["challenges"] is JArray challengesArray)
                {
                    foreach (var challenge in challengesArray)
                    {
                        var challengeStr = challenge.ToString();
                        if (!string.IsNullOrEmpty(challengeStr))
                        {
                            quest.Challenges.Add(challengeStr);
                        }
                    }
                }
                
                // Handle Emotional Beats
                if (questData["emotionalBeats"] is JArray beatsArray)
                {
                    foreach (var beat in beatsArray)
                    {
                        var beatStr = beat.ToString();
                        if (!string.IsNullOrEmpty(beatStr))
                        {
                            quest.EmotionalBeats.Add(beatStr);
                        }
                    }
                }
                
                // Handle Rewards - now a simple list of strings
                if (questData["rewards"] is JArray rewardsArray)
                {
                    foreach (var reward in rewardsArray)
                    {
                        var rewardStr = reward.ToString();
                        if (!string.IsNullOrEmpty(rewardStr))
                        {
                            quest.Rewards.Add(rewardStr);
                        }
                    }
                }
              
                // Save the quest data
                await _storageService.SaveAsync(userId, $"quests/{questId}", quest);
                
                // Check if there are associated entities to create
                if (questData["locationsInvolved"] != null || questData["npcs"] != null)
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
    }
} 