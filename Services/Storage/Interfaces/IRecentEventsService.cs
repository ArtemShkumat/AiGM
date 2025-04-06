using System.Threading.Tasks;
using AiGMBackEnd.Models;

namespace AiGMBackEnd.Services.Storage
{
    /// <summary>
    /// Defines the contract for managing recent event summaries.
    /// </summary>
    public interface IRecentEventsService
    {
        /// <summary>
        /// Loads the recent events summary for a user.
        /// </summary>
        /// <param name="userId">The user/game ID.</param>
        /// <returns>The RecentEvents object, or null if not found.</returns>
        Task<RecentEvents> GetRecentEventsAsync(string userId);

        /// <summary>
        /// Adds a summary entry to the recent events log for a user.
        /// </summary>
        /// <param name="userId">The user/game ID.</param>
        /// <param name="summary">The summary text to add.</param>
        /// <returns>Task representing the asynchronous operation.</returns>
        Task AddSummaryToRecentEventsAsync(string userId, string summary);
    }
} 