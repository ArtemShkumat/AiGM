using System;
using System.IO;
using System.Threading.Tasks;
using AiGMBackEnd.Models;

namespace AiGMBackEnd.Services.Storage
{
    /// <summary>
    /// Service for managing combat state persistence
    /// </summary>
    public class CombatStateService : ICombatStateService
    {
        private readonly IBaseStorageService _baseStorageService;
        private readonly LoggingService _loggingService;
        
        public CombatStateService(
            IBaseStorageService baseStorageService,
            LoggingService loggingService)
        {
            _baseStorageService = baseStorageService;
            _loggingService = loggingService;
        }
        
        /// <summary>
        /// Gets the file ID for the active combat state
        /// </summary>
        /// <returns>The relative file path for the combat state</returns>
        private string GetCombatStateFileId()
        {
            // Store the active combat state in a well-known location
            // Only one active combat per user is allowed
            return "active_combat";
        }
        
        /// <summary>
        /// Saves a combat state to storage
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="combatState">The combat state to save</param>
        public async Task SaveCombatStateAsync(string userId, CombatState combatState)
        {
            if (combatState == null)
            {
                throw new ArgumentNullException(nameof(combatState), "Combat state cannot be null");
            }
            
            // Ensure the combat state has the user ID set
            if (string.IsNullOrEmpty(combatState.UserId))
            {
                combatState.UserId = userId;
            }
            
            // Ensure the combat ID is set
            if (string.IsNullOrEmpty(combatState.CombatId))
            {
                combatState.CombatId = Guid.NewGuid().ToString();
            }
            
            string fileId = GetCombatStateFileId();
            try
            {
                await _baseStorageService.SaveAsync(userId, fileId, combatState);
                _loggingService.LogInfo($"Saved combat state for user '{userId}' to '{fileId}'");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error saving combat state for user '{userId}': {ex.Message}");
                throw; // Re-throw to allow caller to handle
            }
        }
        
        /// <summary>
        /// Loads the active combat state for a user
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <returns>The combat state or null if not found</returns>
        public async Task<CombatState?> LoadCombatStateAsync(string userId)
        {
            string fileId = GetCombatStateFileId();
            try
            {
                var combatState = await _baseStorageService.LoadAsync<CombatState>(userId, fileId);
                
                // Validate the combat state
                if (combatState != null)
                {
                    _loggingService.LogInfo($"Loaded combat state for user '{userId}' from '{fileId}'. CombatId: {combatState.CombatId}, Active: {combatState.IsActive}");
                    return combatState;
                }
                else
                {
                    _loggingService.LogInfo($"No combat state found for user '{userId}' at '{fileId}'");
                    return null;
                }
            }
            catch (FileNotFoundException)
            {
                // This is expected if no combat is active
                _loggingService.LogInfo($"No combat state file found for user '{userId}'");
                return null;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error loading combat state for user '{userId}': {ex.Message}");
                return null; // Return null on error to prevent cascading failures
            }
        }
        
        /// <summary>
        /// Deletes the combat state file for a user
        /// </summary>
        /// <param name="userId">The user ID</param>
        public async Task DeleteCombatStateAsync(string userId)
        {
            string fileId = GetCombatStateFileId();
            try
            {
                string fullPath = _baseStorageService.GetFilePath(userId, fileId);
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    _loggingService.LogInfo($"Deleted combat state file for user '{userId}' at '{fileId}'");
                }
                else
                {
                    _loggingService.LogInfo($"No combat state file found to delete for user '{userId}' at '{fileId}'");
                }
                
                // Ensure this is awaitable for consistency with other methods
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error deleting combat state for user '{userId}': {ex.Message}");
                throw; // Re-throw to allow caller to handle
            }
        }
    }
} 