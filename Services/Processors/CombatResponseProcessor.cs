using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AiGMBackEnd.Models;
using Newtonsoft.Json.Linq;
using Hangfire;
using AiGMBackEnd.Models.Prompts;

namespace AiGMBackEnd.Services.Processors
{
    public interface ICombatResponseProcessor
    {
        Task<ProcessedResult> ProcessCombatResponseAsync(string llmResponse, string userId);
    }
    
    public class CombatResponseProcessor : ICombatResponseProcessor
    {
        private readonly StorageService _storageService;
        private readonly LoggingService _loggingService;
        private readonly GameNotificationService _gameNotificationService;
        
        public CombatResponseProcessor(
            StorageService storageService,
            LoggingService loggingService,
            GameNotificationService gameNotificationService)
        {
            _storageService = storageService;
            _loggingService = loggingService;
            _gameNotificationService = gameNotificationService;
        }
        
        public async Task<ProcessedResult> ProcessCombatResponseAsync(string llmResponse, string userId)
        {
            try
            {
                _loggingService.LogInfo($"Processing combat response for user {userId}");
                
                if (string.IsNullOrWhiteSpace(llmResponse))
                {
                    _loggingService.LogWarning($"Received empty or null combat LLM response for user {userId}.");
                    return new ProcessedResult 
                    { 
                        UserFacingText = "Received an empty response from the combat system.", 
                        Success = false, 
                        ErrorMessage = "Empty combat LLM response." 
                    };
                }
                
                // Parse the JSON response
                JObject responseObject;
                try
                {
                    responseObject = JObject.Parse(llmResponse);
                }
                catch (Exception ex)
                {
                    _loggingService.LogError($"Failed to parse combat response JSON for user {userId}: {ex.Message}. Raw response: {llmResponse}");
                    return new ProcessedResult 
                    { 
                        UserFacingText = "Error processing combat turn (Invalid Format).", 
                        Success = false, 
                        ErrorMessage = $"JSON Parse Error: {ex.Message}" 
                    };
                }
                
                // Extract required fields
                string userFacingText = responseObject["userFacingText"]?.ToString();
                bool? combatEnded = responseObject["combatEnded"]?.Value<bool>();
                bool? playerVictory = responseObject["playerVictory"]?.Value<bool>();
                
                if (string.IsNullOrEmpty(userFacingText))
                {
                    _loggingService.LogWarning($"Missing userFacingText in combat response for user {userId}");
                    userFacingText = "The combat continues...";
                }
                
                // Load the current combat state
                var combatState = await _storageService.LoadCombatStateAsync(userId);
                if (combatState == null)
                {
                    _loggingService.LogError($"No combat state found for user {userId} when processing combat response");
                    return new ProcessedResult
                    {
                        UserFacingText = userFacingText,
                        Success = false,
                        ErrorMessage = "Combat state not found"
                    };
                }
                
                // Update the combat log
                combatState.CombatLog.Add(userFacingText);
                
                // Extract and update enemy successes if provided
                if (responseObject["currentEnemySuccesses"] != null)
                {
                    combatState.CurrentEnemySuccesses = responseObject["currentEnemySuccesses"].Value<int>();
                }
                
                // Extract and update player conditions if provided
                if (responseObject["playerConditions"] != null && responseObject["playerConditions"].Type == JTokenType.Array)
                {
                    combatState.PlayerConditions = responseObject["playerConditions"].ToObject<List<string>>();
                }
                
                // Load enemy stat block for the notification
                var enemyStatBlock = await _storageService.LoadEnemyStatBlockAsync(userId, combatState.EnemyStatBlockId);
                if (enemyStatBlock == null)
                {
                    _loggingService.LogWarning($"Enemy stat block not found for {combatState.EnemyStatBlockId} during combat processing");
                }
                
                // Check if combat should end (via response or success count)
                bool isCombatEnding = combatEnded.GetValueOrDefault(false);
                bool isPlayerVictorious = playerVictory.GetValueOrDefault(false);
                
                // Auto-end combat if successes reach the required amount
                if (enemyStatBlock != null && combatState.CurrentEnemySuccesses >= enemyStatBlock.SuccessesRequired)
                {
                    isCombatEnding = true;
                    isPlayerVictorious = true;
                }
                
                // Check if player has too many conditions (defeat)
                int severeConditions = combatState.PlayerConditions.Count(c => c.StartsWith("Severe:"));
                if (severeConditions >= 1 || combatState.PlayerConditions.Count >= 4)
                {
                    isCombatEnding = true;
                    isPlayerVictorious = false;
                }
                
                if (isCombatEnding)
                {
                    // Mark combat as over
                    combatState.IsActive = false;
                    await _storageService.SaveCombatStateAsync(userId, combatState);
                    
                    // Notify clients combat has ended
                    await _gameNotificationService.NotifyCombatEndedAsync(userId, isPlayerVictorious);
                    
                    // Enqueue a background job to summarize the combat
                    _loggingService.LogInfo($"Combat ended for user {userId}. Enqueuing summarization job. Player victory: {isPlayerVictorious}");
                    var summaryRequest = new PromptRequest
                    {
                        PromptType = PromptType.SummarizeCombat,
                        UserId = userId,
                        Context = isPlayerVictorious.ToString() // Pass victory status via Context
                    };
                    // We need a method in HangfireJobsService to handle this
                    BackgroundJob.Enqueue<HangfireJobsService>(x => x.ProcessSummarizationJobAsync(summaryRequest)); 

                    // DO NOT delete CombatState here, the summary job needs it.
                }
                else
                {
                    // Save updated combat state
                    await _storageService.SaveCombatStateAsync(userId, combatState);
                    
                    // Send combat turn update notification
                    if (enemyStatBlock != null)
                    {
                        await _gameNotificationService.NotifyCombatTurnUpdateAsync(userId, new CombatTurnInfo
                        {
                            CombatId = combatState.CombatId,
                            CurrentEnemySuccesses = combatState.CurrentEnemySuccesses,
                            SuccessesRequired = enemyStatBlock.SuccessesRequired,
                            PlayerConditions = combatState.PlayerConditions
                        });
                    }
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
                _loggingService.LogError($"Error processing combat response for user {userId}: {ex.Message}");
                return new ProcessedResult
                {
                    UserFacingText = "An error occurred during the combat turn.",
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }
    }
} 