using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AiGMBackEnd.Models;
using Newtonsoft.Json.Linq;

namespace AiGMBackEnd.Services.Processors
{
    public interface IEnemyStatBlockProcessor
    {
        Task ProcessAsync(JObject jsonObject, string userId);
    }
    
    public class EnemyStatBlockProcessor : IEnemyStatBlockProcessor
    {
        private readonly StorageService _storageService;
        private readonly LoggingService _loggingService;
        
        public EnemyStatBlockProcessor(StorageService storageService, LoggingService loggingService)
        {
            _storageService = storageService;
            _loggingService = loggingService;
        }
        
        public async Task ProcessAsync(JObject jsonObject, string userId)
        {
            try
            {
                _loggingService.LogInfo($"Processing enemy stat block for user {userId}");
                
                // Validate JSON has required properties
                var requiredProps = new[] { "id", "name", "level", "vulnerability", "badStuff", "description" };
                foreach (var prop in requiredProps)
                {
                    if (jsonObject[prop] == null)
                    {
                        throw new ArgumentException($"Enemy stat block JSON missing required property: {prop}");
                    }
                }
                
                // Convert JObject to EnemyStatBlock
                EnemyStatBlock statBlock = new EnemyStatBlock
                {
                    Id = jsonObject["id"]?.ToString(),
                    Name = jsonObject["name"]?.ToString(),
                    Level = int.TryParse(jsonObject["level"]?.ToString(), out int level) ? level : 1,
                    Description = jsonObject["description"]?.ToString() ?? string.Empty,
                    Vulnerability = jsonObject["vulnerability"]?.ToString() ?? string.Empty,
                    BadStuff = jsonObject["badStuff"]?.ToString() ?? string.Empty
                };
                
                // Parse optional Tags array
                if (jsonObject["tags"] != null && jsonObject["tags"].Type == JTokenType.Array)
                {
                    statBlock.Tags = jsonObject["tags"].ToObject<List<string>>() ?? new List<string>();
                }
                
                // Ensure level is within bounds (1-10)
                if (statBlock.Level < 1) statBlock.Level = 1;
                if (statBlock.Level > 10) statBlock.Level = 10;
                
                // Process and save
                await _storageService.SaveEnemyStatBlockAsync(userId, statBlock);
                
                // Optionally add to world entity list if not there already
                if (!string.IsNullOrEmpty(statBlock.Id))
                {
                    try
                    {
                        await _storageService.AddEntityToWorldAsync(
                            userId,
                            statBlock.Id,
                            statBlock.Name,
                            "enemy_stat_block");
                    }
                    catch (Exception worldEx)
                    {
                        // Log but don't fail if world sync fails
                        _loggingService.LogWarning($"Could not add enemy stat block {statBlock.Id} to world: {worldEx.Message}");
                    }
                }
                
                _loggingService.LogInfo($"Successfully processed enemy stat block {statBlock.Id} for user {userId}");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error processing enemy stat block for user {userId}: {ex.Message}");
                throw;
            }
        }
    }
} 