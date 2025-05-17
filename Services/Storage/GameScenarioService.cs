using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AiGMBackEnd.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AiGMBackEnd.Services.Storage
{
    public class GameScenarioService : IGameScenarioService
    {
        private readonly LoggingService _loggingService;
        private readonly string _dataPath;
        private readonly IBaseStorageService _baseStorageService;

        public GameScenarioService(LoggingService loggingService, IBaseStorageService baseStorageService)
        {
            _loggingService = loggingService;
            _baseStorageService = baseStorageService;
            
            // Set the data path
            string rootDirectory = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.FullName;
            _dataPath = Path.Combine(rootDirectory, "Data");
        }

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
                
                // Ensure a default world.json exists immediately
                var defaultWorldPath = Path.Combine(userGamePath, "world.json");
                if (!File.Exists(defaultWorldPath))
                {
                    // Create a minimal World object
                    var defaultWorld = new World 
                    {
                        Type = "WORLD",
                        GameTime = DateTimeOffset.UtcNow,
                        CurrentPlayer = gameId, 
                        Locations = new List<LocationSummary>(), 
                        Npcs = new List<NpcSummary>(), 
                        Quests = new List<QuestSummary>()
                    };
                    await _baseStorageService.SaveAsync(gameId, "world", defaultWorld);
                    _loggingService.LogInfo($"Created default world.json for game {gameId}");
                }

                // Copy scenario files and folders to the user's game directory
                CopyDirectory(scenarioPath, userGamePath);
                
                // Update player.id and world.currentPlayer to gameId
                await _baseStorageService.ApplyPartialUpdateAsync(gameId, "player", $"{{\"id\": \"{gameId}\"}}");
                await _baseStorageService.ApplyPartialUpdateAsync(gameId, "world", $"{{\"currentPlayer\": \"{gameId}\"}}");
                
                // Save game preferences if provided
                if (preferences != null)
                {
                    await _baseStorageService.SaveAsync(gameId, "gamePreferences", preferences);
                }
                
                // Load events from the scenario
                await LoadEventsFromScenarioAsync(scenarioId, gameId);
                
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
        
        private string GetScenarioBasePath(string scenarioId, string userId, bool isStartingScenario)
        {
            if (isStartingScenario)
            {
                return Path.Combine(_dataPath, "startingScenarios", scenarioId);
            }
            else
            {
                return Path.Combine(_dataPath, "userData", userId);
            }
        }
        
        public async Task CreateScenarioFolderStructureAsync(string scenarioId, string userId, bool isStartingScenario)
        {
            try
            {
                string basePath = GetScenarioBasePath(scenarioId, userId, isStartingScenario);
                
                // Create base directory
                Directory.CreateDirectory(basePath);

                // Create sub-folders for locations, npcs, and events
                Directory.CreateDirectory(Path.Combine(basePath, "locations"));
                Directory.CreateDirectory(Path.Combine(basePath, "npcs"));
                Directory.CreateDirectory(Path.Combine(basePath, "events"));
                
                if (isStartingScenario)
                {
                    _loggingService.LogInfo($"Created starting scenario folder structure for {scenarioId}");
                }
                else
                {
                    _loggingService.LogInfo($"Created user scenario folder structure for user {userId}");
                }
                
                await Task.CompletedTask; // Just to keep the method async for consistency
            }
            catch (Exception ex)
            {
                string logMessage = isStartingScenario
                    ? $"Error creating starting scenario folder structure for {scenarioId}: {ex.Message}"
                    : $"Error creating user scenario folder structure for user {userId}: {ex.Message}";
                
                _loggingService.LogError(logMessage);
                throw;
            }
        }
        
        public async Task SaveScenarioFileAsync(string scenarioId, string fileName, JToken jsonData, string userId, bool isStartingScenario)
        {
            try
            {
                string basePath = GetScenarioBasePath(scenarioId, userId, isStartingScenario);
                
                string filePath = Path.Combine(basePath, fileName);
                await File.WriteAllTextAsync(filePath, jsonData.ToString(Newtonsoft.Json.Formatting.Indented));
                
                string logMessage = isStartingScenario
                    ? $"Saved file {fileName} for starting scenario {scenarioId}"
                    : $"Saved file {fileName} for user {userId}";
                
                _loggingService.LogInfo(logMessage);
            }
            catch (Exception ex)
            {
                string logMessage = isStartingScenario
                    ? $"Error saving {fileName} for starting scenario {scenarioId}: {ex.Message}"
                    : $"Error saving {fileName} for user {userId}: {ex.Message}";
                
                _loggingService.LogError(logMessage);
                throw;
            }
        }
        
        public async Task SaveScenarioLocationAsync(string scenarioId, string locationId, JToken locationData, string userId, bool isStartingScenario)
        {
            try
            {
                string basePath = GetScenarioBasePath(scenarioId, userId, isStartingScenario);
                
                string locationFilePath = Path.Combine(basePath, "locations", $"{locationId}.json");
                await File.WriteAllTextAsync(locationFilePath, locationData.ToString(Newtonsoft.Json.Formatting.Indented));
                
                string logMessage = isStartingScenario
                    ? $"Saved location {locationId} for starting scenario {scenarioId}"
                    : $"Saved location {locationId} for user {userId}";
                
                _loggingService.LogInfo(logMessage);
            }
            catch (Exception ex)
            {
                string logMessage = isStartingScenario
                    ? $"Error saving location {locationId} for starting scenario {scenarioId}: {ex.Message}"
                    : $"Error saving location {locationId} for user {userId}: {ex.Message}";
                
                _loggingService.LogError(logMessage);
                throw;
            }
        }
        
        public async Task SaveScenarioNpcAsync(string scenarioId, string npcId, JToken npcData, string userId, bool isStartingScenario)
        {
            try
            {
                string basePath = GetScenarioBasePath(scenarioId, userId, isStartingScenario);
                
                string npcFilePath = Path.Combine(basePath, "npcs", $"{npcId}.json");
                await File.WriteAllTextAsync(npcFilePath, npcData.ToString(Newtonsoft.Json.Formatting.Indented));
                
                string logMessage = isStartingScenario
                    ? $"Saved NPC {npcId} for starting scenario {scenarioId}"
                    : $"Saved NPC {npcId} for user {userId}";
                
                _loggingService.LogInfo(logMessage);
            }
            catch (Exception ex)
            {
                string logMessage = isStartingScenario
                    ? $"Error saving NPC {npcId} for starting scenario {scenarioId}: {ex.Message}"
                    : $"Error saving NPC {npcId} for user {userId}: {ex.Message}";
                
                _loggingService.LogError(logMessage);
                throw;
            }
        }
        
        public async Task SaveScenarioEventAsync(string scenarioId, string eventId, JToken eventData, string userId, bool isStartingScenario)
        {
            try
            {
                string basePath = GetScenarioBasePath(scenarioId, userId, isStartingScenario);
                string eventsDir = Path.Combine(basePath, "events");
                
                // Ensure events directory exists
                if (!Directory.Exists(eventsDir))
                {
                    Directory.CreateDirectory(eventsDir);
                }
                
                string filePath = Path.Combine(eventsDir, $"{eventId}.json");
                await File.WriteAllTextAsync(filePath, eventData.ToString(Newtonsoft.Json.Formatting.Indented));
                
                string logMessage = isStartingScenario
                    ? $"Saved event {eventId} for starting scenario {scenarioId}"
                    : $"Saved event {eventId} for user {userId}";
                
                _loggingService.LogInfo(logMessage);
            }
            catch (Exception ex)
            {
                string logMessage = isStartingScenario
                    ? $"Error saving event {eventId} for starting scenario {scenarioId}: {ex.Message}"
                    : $"Error saving event {eventId} for user {userId}: {ex.Message}";
                
                _loggingService.LogError(logMessage);
                throw;
            }
        }
        
        public async Task LoadEventsFromScenarioAsync(string scenarioId, string userId)
        {
            try
            {
                // Check if there are events in the scenario
                string scenarioEventsPath = Path.Combine(_dataPath, "startingScenarios", scenarioId, "events");
                if (!Directory.Exists(scenarioEventsPath))
                {
                    _loggingService.LogInfo($"No events directory found in scenario {scenarioId}");
                    return;
                }
                
                // Get all event files
                var eventFiles = Directory.GetFiles(scenarioEventsPath, "*.json");
                if (eventFiles.Length == 0)
                {
                    _loggingService.LogInfo($"No events found in scenario {scenarioId}");
                    return;
                }
                
                // Create user events directory if it doesn't exist
                string userEventsPath = Path.Combine(_dataPath, "userData", userId, "events");
                if (!Directory.Exists(userEventsPath))
                {
                    Directory.CreateDirectory(userEventsPath);
                }
                
                var currentTime = DateTimeOffset.UtcNow;
                
                // Load and process each event
                foreach (var eventFile in eventFiles)
                {
                    try
                    {
                        string json = await File.ReadAllTextAsync(eventFile);
                        var eventObj = JObject.Parse(json);
                        
                        // Generate a new ID for the event
                        string newEventId = Guid.NewGuid().ToString();
                        eventObj["id"] = newEventId;
                        
                        // Set the event as active
                        eventObj["status"] = EventStatus.Active.ToString();
                        
                        // Set creation time to now
                        eventObj["creationTime"] = currentTime.ToString("o"); // ISO 8601 format
                        
                        // Handle relative time triggers
                        if (eventObj["triggerType"]?.ToString() == "Time")
                        {
                            var triggerValue = eventObj["triggerValue"];
                            if (triggerValue != null && triggerValue["triggerTime"] != null)
                            {
                                string triggerTimeStr = triggerValue["triggerTime"].ToString();
                                
                                // Check if it's a relative time specification (e.g., "+2.days")
                                if (triggerTimeStr.StartsWith("+"))
                                {
                                    // Parse the relative time (format: +X.timeUnit)
                                    string[] parts = triggerTimeStr.Substring(1).Split('.');
                                    if (parts.Length == 2 && double.TryParse(parts[0], out double value))
                                    {
                                        DateTimeOffset newTriggerTime = currentTime;
                                        
                                        // Apply the time delta based on the unit
                                        switch (parts[1].ToLowerInvariant())
                                        {
                                            case "minutes":
                                            case "minute":
                                                newTriggerTime = currentTime.AddMinutes(value);
                                                break;
                                            case "hours":
                                            case "hour":
                                                newTriggerTime = currentTime.AddHours(value);
                                                break;
                                            case "days":
                                            case "day":
                                                newTriggerTime = currentTime.AddDays(value);
                                                break;
                                            default:
                                                _loggingService.LogWarning($"Unknown time unit '{parts[1]}' in relative time trigger");
                                                continue; // Skip this event
                                        }
                                        
                                        // Update the trigger time
                                        triggerValue["triggerTime"] = newTriggerTime.ToString("o");
                                    }
                                    else
                                    {
                                        _loggingService.LogWarning($"Invalid relative time format: {triggerTimeStr}");
                                        continue; // Skip this event
                                    }
                                }
                            }
                        }
                        
                        // Save the event to the user's events directory
                        string userEventPath = Path.Combine(userEventsPath, $"{newEventId}.json");
                        await File.WriteAllTextAsync(userEventPath, eventObj.ToString(Newtonsoft.Json.Formatting.Indented));
                        
                        _loggingService.LogInfo($"Created event {newEventId} for user {userId} from scenario {scenarioId}");
                    }
                    catch (Exception ex)
                    {
                        _loggingService.LogError($"Error processing event file {eventFile}: {ex.Message}");
                        // Continue with other events
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error loading events from scenario {scenarioId} for user {userId}: {ex.Message}");
                // Don't throw, as this shouldn't stop the scenario loading process
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
} 