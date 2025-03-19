using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace AiGMBackEnd.Services
{
    public class StorageService
    {
        private readonly LoggingService _loggingService;
        private readonly string _dataPath;

        public StorageService(LoggingService loggingService)
        {
            _loggingService = loggingService;
            _dataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
        }

        public async Task<T> LoadAsync<T>(string userId, string fileId) where T : class
        {
            try
            {
                var filePath = GetFilePath(userId, fileId);
                
                if (!File.Exists(filePath))
                {
                    return null;
                }

                var json = await File.ReadAllTextAsync(filePath);
                return JsonSerializer.Deserialize<T>(json);
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error loading {fileId}: {ex.Message}");
                throw;
            }
        }

        public async Task SaveAsync<T>(string userId, string fileId, T entity) where T : class
        {
            try
            {
                var filePath = GetFilePath(userId, fileId);
                var directory = Path.GetDirectoryName(filePath);
                
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(entity, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                
                await File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error saving {fileId}: {ex.Message}");
                throw;
            }
        }

        public async Task ApplyPartialUpdateAsync(string userId, string fileId, string jsonPatch)
        {
            try
            {
                // TODO: Implement partial update logic
                // This would merge the patch into the existing JSON file
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error applying partial update to {fileId}: {ex.Message}");
                throw;
            }
        }

        private string GetFilePath(string userId, string fileId)
        {
            if (fileId.Contains('/'))
            {
                // Handle paths like "npcs/npc_001.json"
                return Path.Combine(_dataPath, userId, fileId);
            }
            
            // Handle simple file names like "world.json"
            return Path.Combine(_dataPath, userId, $"{fileId}.json");
        }
    }
}
