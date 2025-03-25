using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AiGMBackEnd.Models;
using AiGMBackEnd.Services.Processors;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AiGMBackEnd.Services
{
    public class ResponseProcessingService
    {
        private readonly StorageService _storageService;
        private readonly LoggingService _loggingService;
        private readonly BackgroundJobService _backgroundJobService;
        private readonly LocationProcessor _locationProcessor;
        private readonly QuestProcessor _questProcessor;
        private readonly NPCProcessor _npcProcessor;
        private readonly PlayerProcessor _playerProcessor;
        private readonly UpdateProcessor _updateProcessor;

        public ResponseProcessingService(
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
            _playerProcessor = new PlayerProcessor(storageService, loggingService);
            _updateProcessor = new UpdateProcessor(storageService, loggingService, backgroundJobService);
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
                if (promptType == PromptType.DM)
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
                        await _updateProcessor.ProcessUpdatesAsync(jsonObject, userId);
                        break;
                    case PromptType.NPC:
                        await _updateProcessor.ProcessUpdatesAsync(jsonObject, userId);
                        break;
                    case PromptType.CreateQuest:
                    case PromptType.CreateQuestJson:
                        await _questProcessor.ProcessAsync(jsonObject, userId);
                        break;
                    case PromptType.CreateNPC:
                    case PromptType.CreateNPCJson:
                        await _npcProcessor.ProcessAsync(jsonObject, userId);
                        break;
                    case PromptType.CreateLocation:
                    case PromptType.CreateLocationJson:
                        await _locationProcessor.ProcessAsync(jsonObject, userId);
                        break;
                    case PromptType.CreatePlayerJson:
                        await _playerProcessor.ProcessAsync(jsonObject, userId);
                        break;
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error processing hidden JSON: {ex.Message}");
                throw;
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
    }
}
