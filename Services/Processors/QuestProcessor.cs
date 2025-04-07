using System;
using System.Threading.Tasks;
using AiGMBackEnd.Models;
using AiGMBackEnd.Services;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json; // Added for deserialization

namespace AiGMBackEnd.Services.Processors
{
    public class QuestProcessor : IQuestProcessor
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
                _loggingService.LogInfo("Processing quest creation using direct deserialization.");

                // Directly deserialize JObject to Quest model
                var quest = questData.ToObject<Models.Quest>();

                // Basic validation
                if (quest == null || string.IsNullOrEmpty(quest.Id))
                {
                    _loggingService.LogError("Failed to deserialize quest data or Quest ID is missing.");
                    string? potentialId = questData["id"]?.ToString();
                    _loggingService.LogWarning($"Attempted quest creation for ID (from JObject): {potentialId ?? "Not Found"}");
                    return;
                }

                // Ensure the Type is set correctly
                if (string.IsNullOrEmpty(quest.Type) || quest.Type != "QUEST")
                {
                    _loggingService.LogWarning($"Quest type mismatch or missing for Quest {quest.Id}. Setting to 'QUEST'.");
                    quest.Type = "QUEST";
                }

                // Save the deserialized quest data
                // Assuming the path format is correct
                await _storageService.SaveAsync(userId, $"quests/{quest.Id}", quest);
                _loggingService.LogInfo($"Successfully processed and saved quest: {quest.Id}");

            }
            catch (JsonSerializationException jsonEx)
            {
                _loggingService.LogError($"JSON deserialization error processing quest creation: {jsonEx.Message}");
                _loggingService.LogInfo($"Problematic JSON data: {questData.ToString()}"); // Use LogInfo
                throw;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error processing quest creation: {ex.Message}");
                _loggingService.LogInfo($"JSON data during error: {questData.ToString()}"); // Use LogInfo
                throw;
            }
        }
    }
} 