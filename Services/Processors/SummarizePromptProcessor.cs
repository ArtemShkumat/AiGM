using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AiGMBackEnd.Models;
using Newtonsoft.Json.Linq;
using AiGMBackEnd.Services.Storage;

namespace AiGMBackEnd.Services.Processors
{
    public class SummarizePromptProcessor : ISummarizePromptProcessor
    {
        private readonly IRecentEventsService _recentEventsService;
        private readonly LoggingService _loggingService;
        private readonly IConversationLogService _conversationLogService;
        private readonly StorageService _storageService;

        public SummarizePromptProcessor(
            IRecentEventsService recentEventsService,
            LoggingService loggingService,
            IConversationLogService conversationLogService,
            StorageService storageService)
        {
            _recentEventsService = recentEventsService;
            _loggingService = loggingService;
            _conversationLogService = conversationLogService;
            _storageService = storageService;
        }

        /// <summary>
        /// Process a general summary and add it to recent events
        /// </summary>
        public async Task ProcessSummaryAsync(string summary, string userId)
        {
            if (string.IsNullOrWhiteSpace(summary))
            {
                _loggingService.LogWarning($"Empty summary text received for user {userId}");
                return;
            }
            
            try
            {
                // Clean the summary text
                summary = CleanSummaryText(summary);
                
                // Add the summary through the RecentEventsService
                await _recentEventsService.AddSummaryToRecentEventsAsync(userId, summary);
                
                // Also add to the DM conversation log
                await _conversationLogService.AddDmMessageAsync(userId, summary);
                
                _loggingService.LogInfo($"Added summary to recent events for user {userId}");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error processing summary for user {userId}: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Process a combat summary after combat has ended
        /// </summary>
        public async Task ProcessCombatSummaryAsync(string summary, string userId, bool playerVictory)
        {
            if (string.IsNullOrWhiteSpace(summary))
            {
                _loggingService.LogWarning($"Empty combat summary text received for user {userId}");
                return;
            }
            
            try
            {
                // Get the combat state if it still exists (it might have been deleted)
                var combatState = await _storageService.LoadCombatStateAsync(userId);
                string enemyName = "the enemy";
                
                if (combatState != null)
                {
                    // Get the enemy name from the stat block if available
                    var enemyStatBlock = await _storageService.LoadEnemyStatBlockAsync(userId, combatState.EnemyStatBlockId);
                    if (enemyStatBlock != null)
                    {
                        enemyName = enemyStatBlock.Name;
                    }
                    
                    // Now that we have a summary, we can safely delete the combat state
                    await _storageService.DeleteCombatStateAsync(userId);
                }
                
                // Format the summary with a prefix indicating the outcome
                string prefixedSummary = playerVictory 
                    ? $"[VICTORY AGAINST {enemyName.ToUpper()}] {summary}" 
                    : $"[DEFEAT BY {enemyName.ToUpper()}] {summary}";
                
                // Add the summary to recent events
                await ProcessSummaryAsync(prefixedSummary, userId);
                
                _loggingService.LogInfo($"Processed combat summary for user {userId} - Player victory: {playerVictory}");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error processing combat summary for user {userId}: {ex.Message}");
                throw;
            }
        }

        private string CleanSummaryText(string summary)
        {
            // Remove any markdown formatting or other unwanted characters
            summary = summary.Trim();
            
            // Remove code block markers if present
            if (summary.StartsWith("```") && summary.EndsWith("```"))
            {
                summary = summary.Substring(3, summary.Length - 6).Trim();
            }
            
            return summary;
        }
    }
} 