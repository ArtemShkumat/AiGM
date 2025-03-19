using System;
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

        public ResponseProcessingService(
            StorageService storageService,
            LoggingService loggingService)
        {
            _storageService = storageService;
            _loggingService = loggingService;
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
                    Success = true
                };
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error processing response: {ex.Message}");
                throw;
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
            // Process DM updates - updating world state, player state, etc.
            _loggingService.LogInfo("Processing DM updates");
            
            // TODO: Implement specific DM update logic
        }

        private async Task ProcessNPCUpdatesAsync(JObject updates, string userId)
        {
            // Process NPC updates - updating NPC state, conversation history, etc.
            _loggingService.LogInfo("Processing NPC updates");
            
            // TODO: Implement specific NPC update logic
        }

        private async Task ProcessQuestCreationAsync(JObject questData, string userId)
        {
            // Process quest creation - creating a new quest
            _loggingService.LogInfo("Processing quest creation");
            
            // TODO: Implement quest creation logic
        }

        private async Task ProcessNPCCreationAsync(JObject npcData, string userId)
        {
            // Process NPC creation - creating a new NPC
            _loggingService.LogInfo("Processing NPC creation");
            
            // TODO: Implement NPC creation logic
        }

        private async Task ProcessLocationCreationAsync(JObject locationData, string userId)
        {
            // Process location creation - creating a new location
            _loggingService.LogInfo("Processing location creation");
            
            // TODO: Implement location creation logic
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
            return (llmResponse, null);
        }
    }
}
