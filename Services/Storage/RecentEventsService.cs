using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AiGMBackEnd.Models;

namespace AiGMBackEnd.Services.Storage
{
    /// <summary>
    /// Service implementation for managing recent event summaries.
    /// </summary>
    public class RecentEventsService : IRecentEventsService
    {
        private readonly IBaseStorageService _baseStorageService;
        private readonly LoggingService _loggingService;
        private const string RecentEventsFileId = "recentEvents";

        public RecentEventsService(IBaseStorageService baseStorageService, LoggingService loggingService)
        {
            _baseStorageService = baseStorageService;
            _loggingService = loggingService;
        }

        /// <summary>
        /// Loads the recent events summary for a user.
        /// </summary>
        public async Task<RecentEvents> GetRecentEventsAsync(string userId)
        {
            try
            {
                return await _baseStorageService.LoadAsync<RecentEvents>(userId, RecentEventsFileId);
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error loading recent events for user {userId}: {ex.Message}");
                // Depending on requirements, might return null or an empty object
                return null; 
            }
        }

        /// <summary>
        /// Adds a summary entry to the recent events log for a user.
        /// </summary>
        public async Task AddSummaryToRecentEventsAsync(string userId, string summary)
        {
            try
            {
                // Load the current RecentEvents
                var recentEvents = await GetRecentEventsAsync(userId);

                // If RecentEvents doesn't exist yet, create a new one
                if (recentEvents == null)
                {
                    recentEvents = new RecentEvents();
                }

                // Add the summary as a new entry
                recentEvents.Messages.Add(new Dictionary<string, string>
                {
                    { "Summary", summary }
                });

                // Save the updated RecentEvents
                await _baseStorageService.SaveAsync(userId, RecentEventsFileId, recentEvents);

                _loggingService.LogInfo($"Added summary to RecentEvents for user {userId}");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error adding summary to RecentEvents for user {userId}: {ex.Message}");
                // Rethrow or handle as appropriate
                throw;
            }
        }
    }
} 