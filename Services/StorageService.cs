using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Collections.Generic;
using AiGMBackEnd.Models;

namespace AiGMBackEnd.Services
{
    public class StorageService
    {
        private readonly LoggingService _loggingService;
        private readonly string _dataPath;
        private readonly string _promptTemplatesPath;

        public StorageService(LoggingService loggingService)
        {
            _loggingService = loggingService;
            
            // Change from using the runtime directory to using a Data folder in the project root
            string rootDirectory = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.FullName;
            _dataPath = Path.Combine(rootDirectory, "Data");
            _promptTemplatesPath = Path.Combine(rootDirectory, "PromptTemplates");
        }

        #region Entity-specific accessor methods

        // Player accessors
        public async Task<Player> GetPlayerAsync(string userId)
        {
            return await LoadAsync<Player>(userId, "player");
        }

        // World accessors
        public async Task<World> GetWorldAsync(string userId)
        {
            return await LoadAsync<World>(userId, "world");
        }

        // Game Setting accessors
        public async Task<GameSetting> GetGameSettingAsync(string userId)
        {
            return await LoadAsync<GameSetting>(userId, "gameSetting");
        }

        // Game Preferences accessors
        public async Task<GamePreferences> GetGamePreferencesAsync(string userId)
        {
            return await LoadAsync<GamePreferences>(userId, "gamePreferences");
        }

        // Location accessors
        public async Task<Location> GetLocationAsync(string userId, string locationId)
        {
            return await LoadAsync<Location>(userId, $"locations\\{locationId}");
        }

        // NPC accessors
        public async Task<Npc> GetNpcAsync(string userId, string npcId)
        {
            return await LoadAsync<Npc>(userId, $"npcs/{npcId}");
        }

        // Quest accessors
        public async Task<Quest> GetQuestAsync(string userId, string questId)
        {
            return await LoadAsync<Quest>(userId, $"quests/{questId}");
        }

        #endregion

        #region Template access methods

        public async Task<string> GetTemplateAsync(string templatePath)
        {
            var fullPath = Path.Combine(_promptTemplatesPath, templatePath);
            if (!File.Exists(fullPath))
            {
                _loggingService.LogWarning($"Template file not found: {fullPath}. Using empty template.");
                return string.Empty;
            }

            return await File.ReadAllTextAsync(fullPath);
        }

        // Specific template accessor methods for different prompt types
        public async Task<string> GetDmTemplateAsync(string templateName)
        {
            return await GetTemplateAsync($"DmPrompt/{templateName}.txt");
        }

        public async Task<string> GetNpcTemplateAsync(string templateName)
        {
            return await GetTemplateAsync($"NPCPrompt/{templateName}.txt");
        }

        public async Task<string> GetCreateQuestTemplateAsync(string templateName)
        {
            return await GetTemplateAsync($"CreateQuest/{templateName}.txt");
        }

        public async Task<string> GetCreateQuestJsonTemplateAsync(string templateName)
        {
            return await GetTemplateAsync($"CreateQuestJson/{templateName}.txt");
        }

        public async Task<string> GetCreateNpcTemplateAsync(string templateName)
        {
            return await GetTemplateAsync($"NPCCreationPrompt/{templateName}.txt");
        }

        public async Task<string> GetCreateNpcJsonTemplateAsync(string templateName)
        {
            return await GetTemplateAsync($"NPCJsonCreationPrompt/{templateName}.txt");
        }

        public async Task<string> GetCreateLocationTemplateAsync(string templateName)
        {
            return await GetTemplateAsync($"CreateLocationPrompt/{templateName}.txt");
        }

        public async Task<string> GetCreateLocationJsonTemplateAsync(string templateName)
        {
            return await GetTemplateAsync($"CreateLocationJson/{templateName}.txt");
        }

        public async Task<string> GetCreatePlayerJsonTemplateAsync(string templateName)
        {
            return await GetTemplateAsync($"CreatePlayerJson/{templateName}.txt");
        }

        #endregion

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
                
                // Save the updated JSON back to the file
                await File.WriteAllTextAsync(filePath, existingObject.ToString(Formatting.Indented));
                
                _loggingService.LogInfo($"Applied partial update to {fileId}");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error applying partial update to {fileId}: {ex.Message}");
                throw;
            }
        }

        private string GetFilePath(string userId, string fileId)
        {
            // Ensure everything goes to Data/userData/{userId}
            if (fileId.Contains('/'))
            {
                // Handle paths like "npcs/npc_001.json"
                return Path.Combine(_dataPath, "userData", userId, fileId);
            }
            
            // Handle simple file names like "world.json"
            return Path.Combine(_dataPath, "userData", userId, $"{fileId}.json");
        }
        
        // Methods to support RPGController
        
        public List<string> GetScenarioIds()
        {
            var scenariosPath = Path.Combine(_dataPath, "startingScenarios");
            if (!Directory.Exists(scenariosPath))
            {
                return new List<string>();
            }
            
            return Directory.GetDirectories(scenariosPath)
                .Select(Path.GetFileName)
                .ToList();
        }
        
        public async Task<T> LoadScenarioSettingAsync<T>(string scenarioId, string fileId) where T : class
        {
            try
            {
                var filePath = Path.Combine(_dataPath, "startingScenarios", scenarioId, fileId);
                
                if (!File.Exists(filePath))
                {
                    return null;
                }

                var json = await File.ReadAllTextAsync(filePath);
                return System.Text.Json.JsonSerializer.Deserialize<T>(json);
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error loading scenario setting {fileId}: {ex.Message}");
                return null;
            }
        }
        
        public async Task<string> CreateGameFromScenarioAsync(string scenarioId, GamePreferences preferences = null)
        {
            try
            {
                var scenarioPath = Path.Combine(_dataPath, "startingScenarios", scenarioId);
                
                if (!Directory.Exists(scenarioPath))
                {
                    throw new DirectoryNotFoundException($"Scenario '{scenarioId}' not found");
                }
                
                // Generate new game ID
                var gameId = Guid.NewGuid().ToString();
                var userGamePath = Path.Combine(_dataPath, "userData", gameId);
                
                // Create user game directory
                Directory.CreateDirectory(userGamePath);
                
                // Copy scenario files and folders to the user's game directory
                CopyDirectory(scenarioPath, userGamePath);
                
                // Save game preferences if provided
                if (preferences != null)
                {
                    await SaveAsync(gameId, "gamePreferences", preferences);
                }
                
                _loggingService.LogInfo($"Created new game with ID: {gameId} based on scenario: {scenarioId}");
                
                return gameId;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error creating game from scenario: {ex.Message}");
                throw;
            }
        }
        
        public List<string> GetGameIds()
        {
            var userDataPath = Path.Combine(_dataPath, "userData");
            if (!Directory.Exists(userDataPath))
            {
                return new List<string>();
            }
            
            return Directory.GetDirectories(userDataPath)
                .Select(Path.GetFileName)
                .ToList();
        }
        
        public async Task<List<NpcInfo>> GetVisibleNpcsInLocationAsync(string gameId, string locationId)
        {
            try
            {
                var visibleNpcs = new List<NpcInfo>();
                var npcsPath = Path.Combine(_dataPath, "userData", gameId, "npcs");
                
                if (!Directory.Exists(npcsPath))
                {
                    return visibleNpcs;
                }
                
                foreach (var npcFile in Directory.GetFiles(npcsPath, "*.json"))
                {
                    try
                    {
                        var npcJson = await File.ReadAllTextAsync(npcFile);
                        var npc = System.Text.Json.JsonSerializer.Deserialize<Npc>(npcJson);
                        
                        if (npc.VisibleToPlayer && npc.CurrentLocationId == locationId)
                        {
                            visibleNpcs.Add(new NpcInfo
                            {
                                Id = npc.Id,
                                Name = npc.Name
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _loggingService.LogWarning($"Error reading NPC file {npcFile}: {ex.Message}");
                    }
                }
                
                return visibleNpcs;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error getting visible NPCs: {ex.Message}");
                throw;
            }
        }
        
        // Helper method to copy directory and its contents
        private void CopyDirectory(string sourceDir, string destinationDir)
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
    
    // Class to use in the StorageService methods
    public class NpcInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }
}
