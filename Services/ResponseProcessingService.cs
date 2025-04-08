using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AiGMBackEnd.Models;
using AiGMBackEnd.Services.Processors;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using AiGMBackEnd.Models;

namespace AiGMBackEnd.Services
{
    public class ResponseProcessingService
    {
        private readonly StorageService _storageService;
        private readonly LoggingService _loggingService;
        private readonly IStatusTrackingService _statusTrackingService;
        private readonly ILocationProcessor _locationProcessor;
        private readonly IQuestProcessor _questProcessor;
        private readonly INPCProcessor _npcProcessor;
        private readonly IPlayerProcessor _playerProcessor;
        private readonly IUpdateProcessor _updateProcessor;
        private readonly ISummarizePromptProcessor _summarizePromptProcessor;
        private readonly JsonSerializerOptions _jsonSerializerOptions;

        public ResponseProcessingService(
            StorageService storageService,
            LoggingService loggingService,
            IStatusTrackingService statusTrackingService,
            IUpdateProcessor updateProcessor,
            ILocationProcessor locationProcessor,
            IQuestProcessor questProcessor,
            INPCProcessor npcProcessor,
            IPlayerProcessor playerProcessor,
            ISummarizePromptProcessor summarizePromptProcessor)
        {
            _storageService = storageService;
            _loggingService = loggingService;
            _statusTrackingService = statusTrackingService;
            _updateProcessor = updateProcessor;
            _locationProcessor = locationProcessor;
            _questProcessor = questProcessor;
            _npcProcessor = npcProcessor;
            _playerProcessor = playerProcessor;
            _summarizePromptProcessor = summarizePromptProcessor;

            _jsonSerializerOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            // Register custom converters
            _jsonSerializerOptions.Converters.Add(new CreationHookConverter());
            _jsonSerializerOptions.Converters.Add(new UpdatePayloadConverter());
            _jsonSerializerOptions.Converters.Add(new UpdatePayloadDictionaryConverter());
            _jsonSerializerOptions.Converters.Add(new CreationHookListConverter());
            _jsonSerializerOptions.Converters.Add(new LlmSafeIntConverter());
        }

        public async Task<ProcessedResult> HandleResponseAsync(string llmResponse, PromptType promptType, string userId, string npcId = null)
        {
            try
            {
                _loggingService.LogInfo($"Processing {promptType} response for user {userId}");

                DmResponse dmResponse = null;
                try
                {
                    if (string.IsNullOrWhiteSpace(llmResponse))
                    {
                        _loggingService.LogWarning($"Received empty or null LLM response for user {userId}.");
                        return new ProcessedResult { UserFacingText = "Received an empty response.", Success = false, ErrorMessage = "Empty LLM response." };
                    }
                    llmResponse = llmResponse.Trim();
                    dmResponse = System.Text.Json.JsonSerializer.Deserialize<DmResponse>(llmResponse, _jsonSerializerOptions);
                }
                catch (System.Text.Json.JsonException jsonEx)
                {
                    _loggingService.LogError($"Failed to deserialize DmResponse JSON for user {userId}: {jsonEx.Message}. Raw response: {llmResponse}");
                    return new ProcessedResult { UserFacingText = "Error processing game state update (Invalid Format).", Success = false, ErrorMessage = $"JSON Deserialization Error: {jsonEx.Message}" };
                }
                catch (Exception ex)
                {
                    _loggingService.LogError($"Unexpected error during DmResponse deserialization for user {userId}: {ex.Message}. Raw response: {llmResponse}");
                    return new ProcessedResult { UserFacingText = "Error processing game state update (Internal Error).", Success = false, ErrorMessage = $"Deserialization Error: {ex.Message}" };
                }

                if (dmResponse == null)
                {
                    _loggingService.LogError($"DmResponse deserialization resulted in null for user {userId}. Raw response: {llmResponse}");
                    return new ProcessedResult { UserFacingText = "Error processing game state update (Null Response).", Success = false, ErrorMessage = "Deserialized DmResponse was null." };
                }

                if (dmResponse.NewEntities != null || dmResponse.PartialUpdates != null)
                {
                    await _updateProcessor.ProcessUpdatesAsync(dmResponse.NewEntities, dmResponse.PartialUpdates, userId);
                }
                else
                {
                    _loggingService.LogInfo($"No new entities or partial updates in DmResponse for user {userId}");
                }

                string userFacingText = dmResponse.UserFacingText ?? string.Empty;
                if (promptType == PromptType.DM)
                {
                    await _storageService.AddDmMessageAsync(userId, userFacingText);
                }
                else if (promptType == PromptType.NPC && npcId != null)
                {
                    await _storageService.AddDmMessageToNpcLogAsync(userId, npcId, userFacingText);
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
                _loggingService.LogError($"Error processing response in HandleResponseAsync for user {userId}: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return new ProcessedResult
                {
                    UserFacingText = "An unexpected error occurred while processing the game's response.",
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
                
                string jsonContent = CleanJsonResponse(llmResponse);

                try
                {
                    using var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonContent);
                }
                catch (System.Text.Json.JsonException ex)
                {
                    _loggingService.LogError($"Invalid JSON in create response: {ex.Message}. Raw cleaned content: {jsonContent}");
                    return new ProcessedResult { UserFacingText = string.Empty, Success = false, ErrorMessage = $"Invalid JSON: {ex.Message}" };
                }

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
                _loggingService.LogError($"Error processing create response for {promptType}, user {userId}: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return new ProcessedResult { UserFacingText = string.Empty, Success = false, ErrorMessage = $"Creation Error: {ex.Message}" };
            }
        }

        private string CleanJsonResponse(string jsonResponse)
        {
            if (string.IsNullOrWhiteSpace(jsonResponse)) return string.Empty;
            var cleaned = Regex.Replace(jsonResponse, @"^```(json)?|```$", "", RegexOptions.Multiline).Trim();
            return cleaned;
        }

        private async Task ProcessHiddenJsonAsync(string jsonContent, PromptType promptType, string userId)
        {
            try
            {
                if (promptType == PromptType.DM || promptType == PromptType.NPC)
                {
                    _loggingService.LogError($"ProcessHiddenJsonAsync incorrectly called for {promptType}. Should be handled by HandleResponseAsync directly.");
                    throw new InvalidOperationException($"ProcessHiddenJsonAsync should not be called for {promptType} after refactoring.");
                }

                JObject jsonObject;
                try
                {
                    jsonObject = JObject.Parse(jsonContent);
                }
                catch (Newtonsoft.Json.JsonException ex)
                {
                    _loggingService.LogError($"Failed to parse JSON in ProcessHiddenJsonAsync (Newtonsoft): {ex.Message}. Content: {jsonContent}");
                    jsonContent = jsonContent.Trim();
                    jsonObject = JObject.Parse(jsonContent);
                }

                switch (promptType)
                {
                    case PromptType.CreateQuest:
                        await _questProcessor.ProcessAsync(jsonObject, userId);
                        break;
                    case PromptType.CreateNPC:
                        await _npcProcessor.ProcessAsync(jsonObject, userId);
                        break;
                    case PromptType.CreateLocation:
                        await _locationProcessor.ProcessAsync(jsonObject, userId);
                        break;
                    case PromptType.CreatePlayer:
                        await _playerProcessor.ProcessAsync(jsonObject, userId);
                        break;
                    default:
                        _loggingService.LogWarning($"Unhandled PromptType in ProcessHiddenJsonAsync: {promptType}");
                        break;
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error processing hidden JSON for {promptType}: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Handles the response from a summarization prompt
        /// </summary>
        public async Task<ProcessedResult> HandleSummaryResponseAsync(string llmResponse, string userId)
        {
            try
            {
                _loggingService.LogInfo($"Processing summarization response for user {userId}");
                
                if (string.IsNullOrEmpty(llmResponse))
                {
                    _loggingService.LogWarning("Empty summarization response received");
                    return new ProcessedResult
                    {
                        UserFacingText = string.Empty,
                        Success = false,
                        ErrorMessage = "Empty summarization response"
                    };
                }
                
                string summary = CleanJsonResponse(llmResponse).Trim();
                
                await _summarizePromptProcessor.ProcessSummaryAsync(summary, userId);
                
                return new ProcessedResult
                {
                    UserFacingText = summary,
                    Success = true,
                    ErrorMessage = string.Empty
                };
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error processing summarization response: {ex.Message}");
                return new ProcessedResult
                {
                    UserFacingText = string.Empty,
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }
    }
}
