using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AiGMBackEnd.Models;
using AiGMBackEnd.Services.Processors;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hangfire;

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
        private readonly IEnemyStatBlockProcessor _enemyStatBlockProcessor;
        private readonly ICombatResponseProcessor _combatResponseProcessor;
        private readonly ILlmResponseDeserializer _llmResponseDeserializer;
        private readonly GameNotificationService _gameNotificationService;
        private readonly IScenarioProcessor _scenarioProcessor;

        // Deserialization timeout constant
        private static readonly TimeSpan DeserializationTimeout = TimeSpan.FromSeconds(30);

        public ResponseProcessingService(
            StorageService storageService,
            LoggingService loggingService,
            IStatusTrackingService statusTrackingService,
            IUpdateProcessor updateProcessor,
            ILocationProcessor locationProcessor,
            IQuestProcessor questProcessor,
            INPCProcessor npcProcessor,
            IPlayerProcessor playerProcessor,
            ISummarizePromptProcessor summarizePromptProcessor,
            IEnemyStatBlockProcessor enemyStatBlockProcessor,
            ICombatResponseProcessor combatResponseProcessor,
            ILlmResponseDeserializer llmResponseDeserializer,
            GameNotificationService gameNotificationService,
            IScenarioProcessor scenarioProcessor)
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
            _enemyStatBlockProcessor = enemyStatBlockProcessor;
            _combatResponseProcessor = combatResponseProcessor;
            _llmResponseDeserializer = llmResponseDeserializer;
            _gameNotificationService = gameNotificationService;
            _scenarioProcessor = scenarioProcessor;
        }

        public async Task<ProcessedResult> HandleResponseAsync(string llmResponse, PromptType promptType, string userId, string npcId = null)
        {
            try
            {
                _loggingService.LogInfo($"Processing {promptType} response for user {userId}");

                // For Combat prompts, delegate to the combat processor
                if (promptType == PromptType.Combat)
                {
                    return await HandleCombatResponseAsync(llmResponse, userId);
                }

                // Early check for empty response
                if (string.IsNullOrWhiteSpace(llmResponse))
                {
                    _loggingService.LogWarning($"Received empty or null LLM response for user {userId}.");
                    return new ProcessedResult 
                    { 
                        UserFacingText = "Received an empty response.", 
                        Success = false, 
                        ErrorMessage = "Empty LLM response." 
                    };
                }

                // Attempt to deserialize the response with timeout protection
                var (deserializationSuccess, dmResponse, deserializationError) = 
                    await _llmResponseDeserializer.TryDeserializeAsync<DmResponse>(llmResponse, DeserializationTimeout);

                string extractedUserFacingText = null;
                
                // If deserialization failed, try to salvage the situation
                if (!deserializationSuccess || dmResponse == null)
                {
                    // Try to extract just the userFacingText using regex
                    extractedUserFacingText = ExtractUserFacingTextAsFallback(llmResponse);
                    
                    if (!string.IsNullOrWhiteSpace(extractedUserFacingText))
                    {
                        _loggingService.LogInfo($"Successfully extracted userFacingText via regex: {extractedUserFacingText.Substring(0, Math.Min(50, extractedUserFacingText.Length))}...");
                        
                        // Now try to fix the JSON by replacing the problematic userFacingText with a placeholder
                        string sanitizedJson = System.Text.RegularExpressions.Regex.Replace(
                            llmResponse,
                            @"(""userFacingText""\s*:\s*"").*?("")",
                            "$1placeholder$2",
                            System.Text.RegularExpressions.RegexOptions.Singleline);
                        
                        // Try deserialization again with sanitized JSON
                        (deserializationSuccess, dmResponse, deserializationError) = 
                            await _llmResponseDeserializer.TryDeserializeAsync<DmResponse>(sanitizedJson, DeserializationTimeout);
                        
                        if (deserializationSuccess && dmResponse != null)
                        {
                            _loggingService.LogInfo("Successfully deserialized sanitized JSON after userFacingText extraction");
                        }
                    }
                }
                
                // If we still couldn't deserialize, return an error response
                if (!deserializationSuccess || dmResponse == null)
                {
                    string errorMessage = deserializationError is TimeoutException
                        ? "Deserialization timed out."
                        : $"Deserialization Error: {deserializationError?.Message}";

                    string userFacingMessage = !string.IsNullOrWhiteSpace(extractedUserFacingText)
                        ? extractedUserFacingText  // Use extracted text if available
                        : (deserializationError is TimeoutException
                            ? "Error processing game state update (Timeout)."
                            : "Error processing game state update (Invalid Format).");

                    _loggingService.LogError($"Failed to deserialize DmResponse for user {userId}: {errorMessage}. Raw response: {llmResponse}");
                    
                    return new ProcessedResult 
                    { 
                        UserFacingText = userFacingMessage, 
                        Success = !string.IsNullOrWhiteSpace(extractedUserFacingText), // Mark as success if we got fallback text
                        ErrorMessage = errorMessage 
                    };
                }

                // Process the successfully deserialized DmResponse
                _loggingService.LogInfo($"Successfully deserialized DmResponse for user {userId}. HasPartialUpdates: {dmResponse.PartialUpdates != null}, HasNewEntities: {dmResponse.NewEntities != null && dmResponse.NewEntities.Count > 0}");

                bool hasUpdates = 
                    (dmResponse.NewEntities != null && dmResponse.NewEntities.Count > 0) || 
                    (dmResponse.PartialUpdates != null && (
                        dmResponse.PartialUpdates.Player != null ||
                        dmResponse.PartialUpdates.World != null ||
                        (dmResponse.PartialUpdates.NpcEntries != null && dmResponse.PartialUpdates.NpcEntries.Count > 0) ||
                        (dmResponse.PartialUpdates.LocationEntries != null && dmResponse.PartialUpdates.LocationEntries.Count > 0)
                    ));

                if (hasUpdates)
                {
                    await _updateProcessor.ProcessUpdatesAsync(dmResponse.NewEntities, dmResponse.PartialUpdates, userId);
                }
                else
                {
                    _loggingService.LogInfo($"No new entities or partial updates in DmResponse for user {userId}");
                }

                // If we extracted userFacingText earlier, use that instead of the placeholder
                string userFacingText = !string.IsNullOrWhiteSpace(extractedUserFacingText) 
                    ? extractedUserFacingText 
                    : dmResponse.UserFacingText ?? string.Empty;
                    
                if (promptType == PromptType.DM)
                {
                    await _storageService.AddDmMessageAsync(userId, userFacingText);
                }
                else if (promptType == PromptType.NPC && npcId != null)
                {
                    await _storageService.AddDmMessageToNpcLogAsync(userId, npcId, userFacingText);
                }

                // Check if combat has been triggered
                if (dmResponse.CombatTriggered && !string.IsNullOrEmpty(dmResponse.EnemyToEngageId))
                {
                    _loggingService.LogInfo($"Combat triggered for user {userId} against enemy {dmResponse.EnemyToEngageId}. Enqueuing preparation job.");
                    
                    // Enqueue the Hangfire job to ensure stat block exists and then initiate combat
                    BackgroundJob.Enqueue<HangfireJobsService>(x => 
                        x.EnsureEnemyStatBlockAndInitiateCombatAsync(userId, dmResponse.EnemyToEngageId, userFacingText));

                    // Return immediately, indicating combat is pending
                    return new ProcessedResult
                    {
                        UserFacingText = userFacingText, // The text that initiated combat
                        Success = true,
                        ErrorMessage = string.Empty,
                        CombatPending = true // Signal to the frontend that combat prep is happening
                    };
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

        /// <summary>
        /// Handles responses from Combat prompts, using the CombatResponseProcessor
        /// </summary>
        private async Task<ProcessedResult> HandleCombatResponseAsync(string llmResponse, string userId)
        {
            try
            {
                return await _combatResponseProcessor.ProcessCombatResponseAsync(llmResponse, userId);
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error in HandleCombatResponseAsync for user {userId}: {ex.Message}");
                return new ProcessedResult
                {
                    UserFacingText = "An error occurred during combat processing.",
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<ProcessedResult> HandleCreateResponseAsync(string llmResponse, PromptType promptType, string userId, bool isStartingScenario, string scenarioId)
        {
            try
            {
                _loggingService.LogInfo($"Processing create {promptType} response for user {userId}");
                
                string jsonContent = CleanJsonResponse(llmResponse);

                // Validate JSON using JsonDocument
                try
                {
                    using var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonContent);
                }
                catch (System.Text.Json.JsonException ex)
                {
                    _loggingService.LogError($"Invalid JSON in create response: {ex.Message}. Raw cleaned content: {jsonContent}");
                    return new ProcessedResult { UserFacingText = string.Empty, Success = false, ErrorMessage = $"Invalid JSON: {ex.Message}" };
                }

                // Call the internal processor, passing the scenario context
                await ProcessHiddenJsonAsync(jsonContent, promptType, userId, isStartingScenario, scenarioId);

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

        private async Task ProcessHiddenJsonAsync(string jsonContent, PromptType promptType, string userId, bool isStartingScenario, string scenarioId)
        {
            JObject jsonData = null;
            try
            {
                // Use Newtonsoft.Json for more robust parsing and JObject manipulation
                jsonData = JObject.Parse(jsonContent);

                // Add metadata if this is part of starting scenario creation
                if (isStartingScenario)
                {
                    var metadata = new JObject();
                    metadata["isStartingScenario"] = true;
                    metadata["scenarioId"] = scenarioId;
                    jsonData["metadata"] = metadata;
                    _loggingService.LogInfo($"Added starting scenario metadata. ScenarioId: {scenarioId}");
                }

                // Call the appropriate processor
                switch (promptType)
                {
                    case PromptType.CreateLocation:
                        await _locationProcessor.ProcessAsync(jsonData, userId);
                        break;
                    case PromptType.CreateQuest:
                        await _questProcessor.ProcessAsync(jsonData, userId);
                        break;
                    case PromptType.CreateNPC:
                        await _npcProcessor.ProcessAsync(jsonData, userId);
                        break;
                    case PromptType.CreatePlayer:
                        await _playerProcessor.ProcessAsync(jsonData, userId);
                        break;
                    case PromptType.Summarize:
                        // Handle Summarize using the same method as other types
                        // This should be handled in its own processor eventually
                        _loggingService.LogWarning($"Summarize handling via ProcessHiddenJsonAsync is not yet fully implemented");
                        // Skip calling an unimplemented method
                        break;
                    case PromptType.CreateEnemyStatBlock:
                        await _enemyStatBlockProcessor.ProcessAsync(jsonData, userId);
                        break;
                    case PromptType.BootstrapGameFromSimplePrompt:
                        // Get the scenario ID from the request, assuming it's passed via RequestMetadata
                        var scenarioIdFromMetadata = GetScenarioIdFromMetadata(jsonContent);
                        if (string.IsNullOrEmpty(scenarioIdFromMetadata))
                        {
                            // If not in metadata, try to generate a consistent ID from the content
                            scenarioIdFromMetadata = $"scenario_{DateTime.UtcNow.Ticks}";
                        }
                        
                        // Check if this is a starting scenario from metadata
                        bool isStartingScenarioFromMetadata = false;
                        try 
                        {
                            var metadataObj = jsonData["metadata"] as JObject;
                            if (metadataObj != null && metadataObj["isStartingScenario"] != null)
                            {
                                bool.TryParse(metadataObj["isStartingScenario"].ToString(), out isStartingScenarioFromMetadata);
                            }
                        }
                        catch (Exception ex)
                        {
                            _loggingService.LogWarning($"Error extracting isStartingScenario flag: {ex.Message}");
                        }
                        
                        await _scenarioProcessor.ProcessAsync(jsonData, scenarioIdFromMetadata, userId, isStartingScenarioFromMetadata);
                        break;
                    default:
                        _loggingService.LogWarning($"Unsupported prompt type for ProcessHiddenJsonAsync: {promptType}");
                        break;
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error in ProcessHiddenJsonAsync for {promptType}, user {userId}: {ex.Message}");
                throw;
            }
        }

        // Helper method to extract scenario ID from metadata if available
        private string GetScenarioIdFromMetadata(string jsonContent)
        {
            try
            {
                var jsonObject = JObject.Parse(jsonContent);
                var metadata = jsonObject["metadata"] as JObject;
                if (metadata != null)
                {
                    return metadata["scenarioId"]?.ToString();
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogWarning($"Error extracting scenario ID from metadata: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// Handles the response from a GENERAL summarization prompt (e.g., end of session)
        /// </summary>
        public async Task ProcessGeneralSummaryAsync(string llmResponse, string userId)
        {
            try
            {
                _loggingService.LogInfo($"Processing general summarization response for user {userId}");
                
                if (string.IsNullOrEmpty(llmResponse))
                {
                    _loggingService.LogWarning("Empty general summarization response received");
                    return; // Or throw? Decide error handling
                }
                
                string summary = CleanJsonResponse(llmResponse).Trim();
                
                // Use the injected processor service to handle the summary logic
                await _summarizePromptProcessor.ProcessSummaryAsync(summary, userId);
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error processing general summarization response for {userId}: {ex.Message}");
                // Decide if we should re-throw or just log
                throw; 
            }
        }

        /// <summary>
        /// Handles the response from a COMBAT summarization prompt
        /// </summary>
        public async Task ProcessCombatSummaryAsync(string llmResponse, string userId, bool playerVictory)
        {
             try
            {
                _loggingService.LogInfo($"Processing COMBAT summarization response for user {userId}. Victory: {playerVictory}");
                
                if (string.IsNullOrEmpty(llmResponse))
                {
                    _loggingService.LogWarning("Empty combat summarization response received");
                     return; // Or throw?
                }
                
                string summary = CleanJsonResponse(llmResponse).Trim();

                // Delegate to the specific processor, passing the victory status
                await _summarizePromptProcessor.ProcessCombatSummaryAsync(summary, userId, playerVictory);

                // No longer need comments about passing victory status, as it's now a parameter.
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error processing COMBAT summarization response for {userId}: {ex.Message}");
                // Decide if we should re-throw or just log
                throw; 
            }
        }

        /// <summary>
        /// Extracts just the userFacingText from a JSON response using regex as a fallback
        /// when full JSON deserialization fails.
        /// </summary>
        private string ExtractUserFacingTextAsFallback(string jsonResponse)
        {
            if (string.IsNullOrWhiteSpace(jsonResponse))
            {
                return string.Empty;
            }
            
            try
            {
                // Look for "userFacingText": "some text" pattern
                var match = System.Text.RegularExpressions.Regex.Match(
                    jsonResponse,
                    @"""userFacingText""\s*:\s*""((?:\\.|[^""\\])*)""",
                    System.Text.RegularExpressions.RegexOptions.Singleline);
                
                if (match.Success && match.Groups.Count > 1)
                {
                    string extractedText = match.Groups[1].Value;
                    
                    // Unescape common JSON escape sequences
                    extractedText = System.Text.RegularExpressions.Regex.Replace(
                        extractedText, 
                        @"\\([""\\\/bfnrt])",  // Handle standard JSON escape sequences
                        m => {
                            return m.Groups[1].Value switch
                            {
                                "\"" => "\"",
                                "\\" => "\\",
                                "/" => "/",
                                "b" => "\b",
                                "f" => "\f",
                                "n" => "\n",
                                "r" => "\r",
                                "t" => "\t",
                                _ => m.Groups[1].Value
                            };
                        });
                    
                    // Also handle Unicode escapes like \uXXXX
                    extractedText = System.Text.RegularExpressions.Regex.Replace(
                        extractedText,
                        @"\\u([0-9a-fA-F]{4})",
                        m => {
                            if (int.TryParse(m.Groups[1].Value, System.Globalization.NumberStyles.HexNumber, null, out int unicodeValue))
                            {
                                return char.ConvertFromUtf32(unicodeValue);
                            }
                            return m.Value; // Keep as is if parse fails
                        });
                    
                    _loggingService.LogInfo($"Extracted userFacingText via regex: {extractedText.Substring(0, Math.Min(50, extractedText.Length))}...");
                    return extractedText;
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error extracting userFacingText via regex: {ex.Message}");
            }
            
            return string.Empty;
        }
    }
}
