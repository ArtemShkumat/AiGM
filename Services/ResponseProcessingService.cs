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

        public async Task<ProcessedResult> HandleResponseAsync(string llmResponse, PromptType promptType, string userId, string npcId = null)
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
                    await _storageService.AddDmMessageAsync(userId, userFacingText + " " + hiddenJson);
                }
                else if (promptType == PromptType.NPC)
                {
                    await _storageService.AddDmMessageToNpcLogAsync(userId, npcId, userFacingText + " " + hiddenJson);
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

        public async Task<ProcessedResult> HandleCreateResponseAsync(string llmResponse, PromptType promptType, string userId)
        {
            try
            {
                _loggingService.LogInfo($"Processing create {promptType} response for user {userId}");
                
                // Clean up JSON response
                string jsonContent = CleanJsonResponse(llmResponse);

                // Validate JSON
                try
                {
                    JToken.Parse(jsonContent);
                }
                catch (JsonException ex)
                {
                    _loggingService.LogError($"Invalid JSON in create response: {ex.Message}");
                    return new ProcessedResult
                    {
                        UserFacingText = string.Empty,
                        Success = false,
                        ErrorMessage = $"Invalid JSON: {ex.Message}"
                    };
                }

                // Process the JSON for entity creation
                await ProcessHiddenJsonAsync(jsonContent, promptType, userId);

                return new ProcessedResult
                {
                    UserFacingText = string.Empty,
                    Success = true,
                    ErrorMessage = string.Empty
                };
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error processing create response: {ex.Message}");
                return new ProcessedResult
                {
                    UserFacingText = string.Empty,
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private string CleanJsonResponse(string jsonResponse)
        {
            // Remove any markdown code block formatting
            jsonResponse = Regex.Replace(jsonResponse, @"^```json\s*|\s*```$", string.Empty, RegexOptions.Multiline).Trim();
            
            // Find the start of the actual JSON content
            int jsonStartIndex = jsonResponse.IndexOf('{');
            if (jsonStartIndex == -1)
                jsonStartIndex = jsonResponse.IndexOf('[');

            if (jsonStartIndex >= 0)
            {
                string extractedJson = jsonResponse.Substring(jsonStartIndex).Trim();
                return FixJsonEscaping(extractedJson);
            }
            
            return FixJsonEscaping(jsonResponse.Trim());
        }

        /// <summary>
        /// Fixes common JSON escaping issues, particularly problematic backslashes
        /// </summary>
        private string FixJsonEscaping(string json)
        {
            if (string.IsNullOrEmpty(json))
                return json;

            // Fix improperly escaped backslashes in string content
            // This regex looks for a backslash that isn't followed by a valid escape character
            json = Regex.Replace(json, @"\\(?![\""/\\bfnrtu])", @"\\");

            // Fix common escaped quotes problems like \"Text\" -> "Text"
            json = Regex.Replace(json, @"\\""([^""\\]*?)\\""", "\"$1\"");

            return json;
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
                        await _questProcessor.ProcessAsync(jsonObject, userId);
                        break;
                    case PromptType.CreateNPC:
                        await _npcProcessor.ProcessAsync(jsonObject, userId);
                        break;
                    case PromptType.CreateLocation:
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
                    // Apply JSON escape sequence fixes
                    jsonCandidate = FixJsonEscaping(jsonCandidate);

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
                        
                        // Try more aggressive fixing for specific known issues
                        try
                        {
                            // Fix backslashes in quoted text like \The Weary Wanderer Inn\
                            string fixedJson = Regex.Replace(jsonCandidate, @"\\([A-Za-z0-9 ]+)\\", "\"$1\"");
                            JToken.Parse(fixedJson);
                            _loggingService.LogInfo("Fixed JSON with additional rules");
                            return (userFacingText, fixedJson);
                        }
                        catch
                        {
                            // Return what we have anyway and let the processor handle it
                            return (userFacingText, jsonCandidate);
                        }
                    }
                }

                return (userFacingText, jsonContent);
            }

            // No hidden content found
            return (llmResponse, string.Empty);
        }
    }
}
