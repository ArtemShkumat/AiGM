using System.Threading.Tasks;
using AiGMBackEnd.Models;
using AiGMBackEnd.Services;
using Newtonsoft.Json.Linq;

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
                    CurrentProgress = questData["currentProgress"]?.ToString(),
                    QuestDescription = questData["questDescription"]?.ToString()
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
    }
} 