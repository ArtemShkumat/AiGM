using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AiGMBackEnd.Services.Storage
{
    public class BaseStorageService : IBaseStorageService
    {
        protected readonly LoggingService _loggingService;
        protected readonly string _dataPath;

        public BaseStorageService(LoggingService loggingService)
        {
            _loggingService = loggingService;
            
            // Change from using the runtime directory to using a Data folder in the project root
            string rootDirectory = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.FullName;
            _dataPath = Path.Combine(rootDirectory, "Data");
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
                return System.Text.Json.JsonSerializer.Deserialize<T>(json);
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

                var json = System.Text.Json.JsonSerializer.Serialize(entity, new JsonSerializerOptions
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
                var filePath = GetFilePath(userId, fileId);
                
                if (!File.Exists(filePath))
                {
                    _loggingService.LogWarning($"Cannot apply partial update to non-existent file: {fileId}");
                    
                    // Create the file with just the patch data
                    var directory = Path.GetDirectoryName(filePath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    
                    await File.WriteAllTextAsync(filePath, jsonPatch);
                    return;
                }
                
                // Read the existing JSON file
                var existingJson = await File.ReadAllTextAsync(filePath);
                
                // Parse existing JSON and the patch
                var existingObject = JObject.Parse(existingJson);
                var patchObject = JObject.Parse(jsonPatch);
                
                // Merge the patch into the existing object
                existingObject.Merge(patchObject, new JsonMergeSettings
                {
                    MergeArrayHandling = MergeArrayHandling.Union,
                    MergeNullValueHandling = MergeNullValueHandling.Merge
                });
                
                // Enhanced logging - show merged result for NPC files
                string resultJson = existingObject.ToString(Formatting.Indented);                
                
                // Save the updated JSON back to the file
                await File.WriteAllTextAsync(filePath, resultJson);
                
                _loggingService.LogInfo($"Applied partial update to {fileId}");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error applying partial update to {fileId}: {ex.Message}");
                throw;
            }
        }

        public string GetFilePath(string userId, string fileId)
        {
            // Check if fileId already contains path information (e.g., "locations/loc_id")
            if (fileId.Contains(Path.DirectorySeparatorChar) || fileId.Contains(Path.AltDirectorySeparatorChar))
            {
                // Assume the fileId is relative to the userId directory and includes the .json extension
                return Path.Combine(_dataPath, "userData", userId, $"{fileId}.json");
            }
            else
            {
                // Legacy behavior for top-level files like player.json, world.json
                return Path.Combine(_dataPath, "userData", userId, $"{fileId}.json");
            }
        }
        
        // Helper method to copy directory and its contents
        public void CopyDirectory(string sourceDir, string destinationDir)
        {
            // Create the destination directory if it doesn't exist
            Directory.CreateDirectory(destinationDir);
            
            // Copy all files from source to destination
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var fileName = Path.GetFileName(file);
                var destFile = Path.Combine(destinationDir, fileName);
                File.Copy(file, destFile);
            }
            
            // Copy all subdirectories recursively
            foreach (var directory in Directory.GetDirectories(sourceDir))
            {
                var dirName = Path.GetFileName(directory);
                var destDir = Path.Combine(destinationDir, dirName);
                CopyDirectory(directory, destDir);
            }
        }
    }
} 