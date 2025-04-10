using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AiGMBackEnd.Models;

namespace AiGMBackEnd.Services.Storage
{
    /// <summary>
    /// Service for managing enemy stat block persistence
    /// </summary>
    public class EnemyStatBlockService : IEnemyStatBlockService
    {
        private readonly IBaseStorageService _baseStorageService;
        private readonly LoggingService _loggingService;
        
        public EnemyStatBlockService(
            IBaseStorageService baseStorageService,
            LoggingService loggingService)
        {
            _baseStorageService = baseStorageService;
            _loggingService = loggingService;
        }
        
        /// <summary>
        /// Gets the file ID for an enemy stat block
        /// </summary>
        /// <param name="enemyId">The enemy ID</param>
        /// <returns>The relative file path for the enemy stat block</returns>
        private string GetEnemyStatBlockFileId(string enemyId)
        {
            // Ensure the enemyId starts with "enemy_" or is mapped correctly if needed
            string safeEnemyId = enemyId;
            if (enemyId.StartsWith("npc_"))
            {
                safeEnemyId = $"enemy_{enemyId.Substring(4)}";
            }
            else if (!enemyId.StartsWith("enemy_"))
            {
                // If it's neither npc_ nor enemy_, assume it needs the enemy_ prefix.
                // This might happen if an ID comes from a source not following the convention.
                safeEnemyId = $"enemy_{enemyId}";
                _loggingService.LogWarning($"Enemy ID '{enemyId}' did not start with 'npc_' or 'enemy_'. Assuming prefix 'enemy_' -> '{safeEnemyId}'");
            }
            return Path.Combine("enemies", $"{safeEnemyId}.json");
        }
        
        /// <summary>
        /// Loads an EnemyStatBlock from storage
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="enemyId">The enemy ID</param>
        /// <returns>The enemy stat block or null if not found</returns>
        public async Task<EnemyStatBlock?> LoadEnemyStatBlockAsync(string userId, string enemyId)
        {
            string fileId = GetEnemyStatBlockFileId(enemyId);
            try
            {
                // Ensure the directory exists before trying to load (LoadAsync might not create it)
                string directoryPath = Path.GetDirectoryName(_baseStorageService.GetFilePath(userId, fileId));
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                    _loggingService.LogInfo($"Created directory for enemy stat blocks: {directoryPath}");
                    return null; // Directory didn't exist, so file can't exist
                }
                return await _baseStorageService.LoadAsync<EnemyStatBlock>(userId, fileId);
            }
            catch (FileNotFoundException)
            {
                // This is expected if the stat block hasn't been created yet.
                _loggingService.LogInfo($"Enemy stat block file not found for '{enemyId}' (File ID: '{fileId}') for user '{userId}'. Returning null.");
                return null;
            }
            catch (DirectoryNotFoundException)
            {
                 _loggingService.LogInfo($"Enemy stat block directory not found for '{enemyId}' (File ID: '{fileId}') for user '{userId}'. Returning null.");
                 return null;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error loading enemy stat block '{enemyId}' (File ID: '{fileId}') for user '{userId}': {ex.Message} - StackTrace: {ex.StackTrace}");
                // Returning null for safety in case of deserialization errors etc.
                return null;
            }
        }
        
        /// <summary>
        /// Saves an EnemyStatBlock to storage
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="statBlock">The enemy stat block to save</param>
        public async Task SaveEnemyStatBlockAsync(string userId, EnemyStatBlock statBlock)
        {
            if (statBlock == null || string.IsNullOrWhiteSpace(statBlock.Id))
            {
                throw new ArgumentNullException(nameof(statBlock), "Stat block or its ID cannot be null or empty.");
            }

            // Ensure SuccessesRequired is calculated
            if (statBlock.Level < 1) statBlock.Level = 1; // Ensure level is at least 1
            statBlock.SuccessesRequired = (int)Math.Ceiling(statBlock.Level / 2.0);

            string fileId = GetEnemyStatBlockFileId(statBlock.Id);
            try
            {
                await _baseStorageService.SaveAsync(userId, fileId, statBlock);
                _loggingService.LogInfo($"Saved enemy stat block '{statBlock.Id}' (File ID: '{fileId}') for user '{userId}'.");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error saving enemy stat block '{statBlock.Id}' (File ID: '{fileId}') for user '{userId}': {ex.Message} - StackTrace: {ex.StackTrace}");
                throw; // Re-throw exceptions during save
            }
        }
        
        /// <summary>
        /// Checks if an EnemyStatBlock exists in storage
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="enemyId">The enemy ID</param>
        /// <returns>True if the enemy stat block exists, false otherwise</returns>
        public Task<bool> CheckIfStatBlockExistsAsync(string userId, string enemyId)
        {
            try
            {
                 string fileId = GetEnemyStatBlockFileId(enemyId);
                 string fullPath = _baseStorageService.GetFilePath(userId, fileId);
                 return Task.FromResult(File.Exists(fullPath));
            }
             catch (Exception ex)
            {
                _loggingService.LogError($"Error checking existence of enemy stat block '{enemyId}' for user '{userId}': {ex.Message}");
                return Task.FromResult(false); // Return false on error
            }
        }
    }
} 