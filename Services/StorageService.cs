using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Collections.Generic;
using AiGMBackEnd.Models;
using AiGMBackEnd.Models.Locations;

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
            return await LoadAsync<Location>(userId, $"locations/{locationId}");
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

        // Conversation Log accessors
        public async Task<ConversationLog> GetConversationLogAsync(string userId)
        {
            var log = await LoadAsync<ConversationLog>(userId, "conversationLog");
            
            // If the log doesn't exist yet, create a new one
            if (log == null)
            {
                log = new ConversationLog();
                await SaveAsync(userId, "conversationLog", log);
            }
            
            return log;
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
            return await GetTemplateAsync($"Create/Quest/{templateName}.txt");
        }

        public async Task<string> GetCreateNpcTemplateAsync(string templateName)
        {
            return await GetTemplateAsync($"Create/NPC/{templateName}.txt");
        }


        public async Task<string> GetCreateLocationTemplateAsync(string templateName, string locationType = null)
        {
            if (string.IsNullOrEmpty(locationType))
            {
                // Default to general location template
                return await GetTemplateAsync($"Create/Location/{templateName}.txt");
            }
            else
            {
                // Use location type specific template
                return await GetTemplateAsync($"Create/Location/{locationType}/{templateName}.txt");
            }
        }

        public async Task<string> GetCreatePlayerJsonTemplateAsync(string templateName)
        {
            return await GetTemplateAsync($"Create/Player/{templateName}.txt");
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
                
                // Use custom options if we're deserializing a Location
                if (typeof(T) == typeof(Location) || typeof(Location).IsAssignableFrom(typeof(T)))
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    
                    return System.Text.Json.JsonSerializer.Deserialize<T>(json, options);
                }
                
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
            //if (fileId.Contains('/'))
            //{
            //    // Handle paths like "npcs/npc_001.json"
            //    return Path.Combine(_dataPath, "userData", userId, fileId);
            //}
            
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
                
                // Update player.id and world.currentPlayer to gameId
                await ApplyPartialUpdateAsync(gameId, "player", $"{{\"id\": \"{gameId}\"}}");
                await ApplyPartialUpdateAsync(gameId, "world", $"{{\"currentPlayer\": \"{gameId}\"}}");
                
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
        
        // Method to get all NPCs in a location with full details
        public async Task<List<Npc>> GetNpcsInLocationAsync(string userId, string locationId)
        {
            try
            {
                var npcsInLocation = new List<Npc>();
                var npcsPath = Path.Combine(_dataPath, "userData", userId, "npcs");
                
                if (!Directory.Exists(npcsPath))
                {
                    return npcsInLocation;
                }
                
                foreach (var npcFile in Directory.GetFiles(npcsPath, "*.json"))
                {
                    try
                    {
                        var npcJson = await File.ReadAllTextAsync(npcFile);
                        var npc = System.Text.Json.JsonSerializer.Deserialize<Npc>(npcJson);
                        
                        if (npc.CurrentLocationId == locationId)
                        {
                            npcsInLocation.Add(npc);
                        }
                    }
                    catch (Exception ex)
                    {
                        _loggingService.LogWarning($"Error reading NPC file {npcFile}: {ex.Message}");
                    }
                }
                
                return npcsInLocation;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error getting NPCs in location: {ex.Message}");
                throw;
            }
        }
        
        // Method to get all NPCs in a game
        public async Task<List<Npc>> GetAllNpcsAsync(string gameId)
        {
            try
            {
                var allNpcs = new List<Npc>();
                var npcsPath = Path.Combine(_dataPath, "userData", gameId, "npcs");
                
                if (!Directory.Exists(npcsPath))
                {
                    return allNpcs;
                }
                
                foreach (var npcFile in Directory.GetFiles(npcsPath, "*.json"))
                {
                    try
                    {
                        var npcJson = await File.ReadAllTextAsync(npcFile);
                        var npc = System.Text.Json.JsonSerializer.Deserialize<Npc>(npcJson);
                        allNpcs.Add(npc);
                    }
                    catch (Exception ex)
                    {
                        _loggingService.LogWarning($"Error reading NPC file {npcFile}: {ex.Message}");
                    }
                }
                
                return allNpcs;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error getting all NPCs: {ex.Message}");
                throw;
            }
        }
        
        // Method to get all visible NPCs in a game
        public async Task<List<Npc>> GetAllVisibleNpcsAsync(string gameId)
        {
            try
            {
                var allNpcs = await GetAllNpcsAsync(gameId);
                return allNpcs.Where(npc => npc.VisibleToPlayer).ToList();
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error getting visible NPCs: {ex.Message}");
                throw;
            }
        }
        
        // Method to get all active quests for a player
        public async Task<List<Quest>> GetActiveQuestsAsync(string userId, List<string> activeQuestIds)
        {
            try
            {
                var activeQuests = new List<Quest>();
                
                if (activeQuestIds == null || activeQuestIds.Count == 0)
                {
                    return activeQuests;
                }
                
                foreach (var questId in activeQuestIds)
                {
                    try
                    {
                        var quest = await GetQuestAsync(userId, questId);
                        if (quest != null)
                        {
                            activeQuests.Add(quest);
                        }
                    }
                    catch (Exception ex)
                    {
                        _loggingService.LogWarning($"Error fetching quest {questId}: {ex.Message}");
                    }
                }
                
                return activeQuests;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error getting active quests: {ex.Message}");
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

        public async Task AddUserMessageAsync(string userId, string content)
        {
            var log = await GetConversationLogAsync(userId);
            
            log.Messages.Add(new Message
            {
                Sender = "user",
                Content = content
            });
            
            await SaveAsync(userId, "conversationLog", log);
        }

        public async Task AddDmMessageAsync(string userId, string content)
        {
            var log = await GetConversationLogAsync(userId);
            
            log.Messages.Add(new Message
            {
                Sender = "dm",
                Content = content
            });
            
            await SaveAsync(userId, "conversationLog", log);
        }

        public async Task AddUserMessageToNpcLogAsync(string userId, string npcId, string content)
        {
            var npc = await GetNpcAsync(userId, npcId);
            
            if (npc == null)
            {
                _loggingService.LogWarning($"Attempted to add user message to non-existent NPC log: {npcId}");
                return;
            }
            
            var messageEntry = new Dictionary<string, string>
            {
                { "timestamp", DateTime.UtcNow.ToString("o") },
                { "sender", "user" },
                { "content", content }
            };
            
            npc.ConversationLog.Add(messageEntry);
            
            await SaveAsync(userId, $"npcs/{npcId}", npc);
        }

        public async Task AddDmMessageToNpcLogAsync(string userId, string npcId, string content)
        {
            var npc = await GetNpcAsync(userId, npcId);
            
            if (npc == null)
            {
                _loggingService.LogWarning($"Attempted to add DM message to non-existent NPC log: {npcId}");
                return;
            }
            
            var messageEntry = new Dictionary<string, string>
            {
                { "timestamp", DateTime.UtcNow.ToString("o") },
                { "sender", npcId },
                { "content", content }
            };
            
            npc.ConversationLog.Add(messageEntry);
            
            await SaveAsync(userId, $"npcs/{npcId}", npc);
        }

        #region World Entity Management

        public async Task AddEntityToWorldAsync(string userId, string entityId, string entityName, string entityType)
        {
            try
            {
                var world = await GetWorldAsync(userId);
                if (world == null)
                {
                    _loggingService.LogWarning($"World object not found for user {userId}");
                    return;
                }

                bool entityAdded = false;
                
                switch (entityType.ToLower())
                {
                    case "npc":
                        if (world.Npcs == null)
                        {
                            world.Npcs = new List<NpcSummary>();
                        }
                        
                        if (!world.Npcs.Any(n => n.Id == entityId))
                        {
                            world.Npcs.Add(new NpcSummary
                            {
                                Id = entityId,
                                Name = entityName
                            });
                            entityAdded = true;
                        }
                        break;
                        
                    case "location":
                        if (world.Locations == null)
                        {
                            world.Locations = new List<LocationSummary>();
                        }
                        
                        if (!world.Locations.Any(l => l.Id == entityId))
                        {
                            world.Locations.Add(new LocationSummary
                            {
                                Id = entityId,
                                Name = entityName
                            });
                            entityAdded = true;
                        }
                        break;
                        
                    case "quest":
                        if (world.Quests == null)
                        {
                            world.Quests = new List<QuestSummary>();
                        }
                        
                        if (!world.Quests.Any(q => q.Id == entityId))
                        {
                            world.Quests.Add(new QuestSummary
                            {
                                Id = entityId,
                                Title = entityName
                            });
                            entityAdded = true;
                        }
                        break;
                        
                    default:
                        _loggingService.LogWarning($"Unknown entity type for world addition: {entityType}");
                        break;
                }
                
                if (entityAdded)
                {
                    await SaveAsync(userId, "world", world);
                    _loggingService.LogInfo($"Added {entityType} {entityId} to world for user {userId}");
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error adding entity to world: {ex.Message}");
            }
        }

        #endregion

        #region Validation Methods

        public async Task<List<DanglingReferenceInfo>> FindDanglingReferencesAsync(string userId)
        {
            var userPath = Path.Combine(_dataPath, "userData", userId);
            var danglingReferencesResult = new List<DanglingReferenceInfo>();
            // Dictionary to map ReferenceId -> Set of FilePaths where it was found
            var foundReferencesMap = new Dictionary<string, HashSet<string>>();

            // Define entity prefixes
            string[] entityPrefixes = { "npc_", "loc_", "quest_" };

            try
            {
                // 1. Get existing entity IDs
                var existingNpcIds = GetExistingEntityIds(Path.Combine(userPath, "npcs"));
                var existingLocationIds = GetExistingEntityIds(Path.Combine(userPath, "locations"));
                var existingQuestIds = GetExistingEntityIds(Path.Combine(userPath, "quests"));

                // 2. List files to scan
                var filesToScan = new List<string>();
                var worldFilePath = Path.Combine(userPath, "world.json");
                var playerFilePath = Path.Combine(userPath, "player.json");

                if (File.Exists(worldFilePath)) filesToScan.Add(worldFilePath);
                if (File.Exists(playerFilePath)) filesToScan.Add(playerFilePath);
                if (Directory.Exists(Path.Combine(userPath, "npcs"))) filesToScan.AddRange(Directory.GetFiles(Path.Combine(userPath, "npcs"), "*.json"));
                if (Directory.Exists(Path.Combine(userPath, "locations"))) filesToScan.AddRange(Directory.GetFiles(Path.Combine(userPath, "locations"), "*.json"));
                if (Directory.Exists(Path.Combine(userPath, "quests"))) filesToScan.AddRange(Directory.GetFiles(Path.Combine(userPath, "quests"), "*.json"));

                // 3. Scan files for references
                foreach (var filePath in filesToScan)
                {
                    try
                    {
                        var json = await File.ReadAllTextAsync(filePath);
                        var token = JToken.Parse(json);
                        // Pass filePath to the recursive function
                        FindReferencesRecursive(token, foundReferencesMap, entityPrefixes, filePath);
                    }
                    catch (Newtonsoft.Json.JsonException jsonEx)
                    {
                        _loggingService.LogWarning($"Skipping file due to JSON parse error: {filePath}. Error: {jsonEx.Message}");
                    }
                    catch (Exception fileEx)
                    {
                        _loggingService.LogError($"Error reading or processing file {filePath}: {fileEx.Message}");
                    }
                }

                // 4. Compare found references to existing IDs
                foreach (var kvp in foundReferencesMap)
                {
                    var referenceId = kvp.Key;
                    var filePaths = kvp.Value;

                    bool isDangling = false;
                    if (referenceId.StartsWith("npc_") && !existingNpcIds.Contains(referenceId))
                    {
                        isDangling = true;
                    }
                    else if (referenceId.StartsWith("loc_") && !existingLocationIds.Contains(referenceId))
                    {
                        isDangling = true;
                    }
                    else if (referenceId.StartsWith("quest_") && !existingQuestIds.Contains(referenceId))
                    {
                        isDangling = true;
                    }

                    if (isDangling)
                    {
                        foreach (var filePath in filePaths)
                        {
                            danglingReferencesResult.Add(new DanglingReferenceInfo
                            {
                                ReferenceId = referenceId,
                                FilePath = filePath // Store the file path
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error finding dangling references for user {userId}: {ex.Message}");
                // Optionally rethrow or return an indication of error
            }

            return danglingReferencesResult; // Return the detailed list
        }

        private HashSet<string> GetExistingEntityIds(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                return new HashSet<string>();
            }
            return Directory.GetFiles(directoryPath, "*.json")
                            .Select(Path.GetFileNameWithoutExtension)
                            .ToHashSet();
        }

        // Updated to track file paths
        private void FindReferencesRecursive(JToken token, Dictionary<string, HashSet<string>> referencesMap, string[] prefixes, string filePath)
        {
            if (token == null) return;

            if (token is JValue value && value.Type == JTokenType.String)
            {
                var stringValue = value.ToString();
                foreach (var prefix in prefixes)
                {
                    if (stringValue.StartsWith(prefix))
                    {
                        // If referenceId is not in the map, add it with a new HashSet
                        if (!referencesMap.ContainsKey(stringValue))
                        {
                            referencesMap[stringValue] = new HashSet<string>();
                        }
                        // Add the current file path to the set for this referenceId
                        referencesMap[stringValue].Add(filePath);
                        break; // Found a match, no need to check other prefixes for this string
                    }
                }
            }
            else if (token is JObject obj)
            {
                foreach (var property in obj.Properties())
                {
                    // Pass filePath down recursively
                    FindReferencesRecursive(property.Value, referencesMap, prefixes, filePath);
                }
            }
            else if (token is JArray array)
            {
                foreach (var item in array)
                {
                    // Pass filePath down recursively
                    FindReferencesRecursive(item, referencesMap, prefixes, filePath);
                }
            }
        }

        #endregion
    }
    
    // Class to use in the StorageService methods
    public class NpcInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    // Class to hold information about a dangling reference
    public class DanglingReferenceInfo
    {
        public string ReferenceId { get; set; }
        public string FilePath { get; set; }
    }
}
