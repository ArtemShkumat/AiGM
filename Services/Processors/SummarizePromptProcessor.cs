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

        public SummarizePromptProcessor(
            IRecentEventsService recentEventsService,
            LoggingService loggingService,
            IConversationLogService conversationLogService)
        {
            _recentEventsService = recentEventsService;
            _loggingService = loggingService;
            _conversationLogService = conversationLogService;
        }

        public async Task ProcessSummaryAsync(string summary, string userId)
        {
            try
            {
                _loggingService.LogInfo($"Processing summary for user {userId}");
                
                if (string.IsNullOrEmpty(summary))
                {
                    _loggingService.LogWarning("Empty summary received");
                    return;
                }

                // Clean the summary text - remove any extra formatting or quotation marks
                summary = CleanSummaryText(summary);
                
                // Add the summary to the RecentEvents using the service
                await _recentEventsService.AddSummaryToRecentEventsAsync(userId, summary);
                
                // Wipe the conversation log, keeping only the last message
                await _conversationLogService.WipeLogAsync(userId);
                
                _loggingService.LogInfo($"Successfully processed summary and wiped log for user {userId}");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error processing summary: {ex.Message}");
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