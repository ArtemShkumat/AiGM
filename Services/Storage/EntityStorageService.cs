using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AiGMBackEnd.Models;
using AiGMBackEnd.Models.Locations;
using AiGMBackEnd.Services;

namespace AiGMBackEnd.Services.Storage
{
    public class EntityStorageService : BaseStorageService, IEntityStorageService
    {
        private readonly IGameScenarioService _gameScenarioService;

        public EntityStorageService(LoggingService loggingService, IGameScenarioService gameScenarioService) : base(loggingService)
        {
            _gameScenarioService = gameScenarioService;
        }

        #region Entity-specific accessor methods

        // Player accessors
        public async Task<Player> GetPlayerAsync(string userId)
        {
            return await LoadAsync<Player>(userId, "player");
        }

        // World accessors
        public async Task<World> GetWorldAsync(string userId, string scenarioId = null)
        {
            // If scenarioId is provided, load from scenario data
            if (!string.IsNullOrEmpty(scenarioId))
            {
                return await _gameScenarioService.LoadScenarioSettingAsync<World>(scenarioId, "world.json");
            }
            
            // Otherwise, load from user data
            return await LoadAsync<World>(userId, "world");
        }

        // Game Setting accessors
        public async Task<GameSetting> GetGameSettingAsync(string userId, string scenarioId = null)
        {
            // If scenarioId is provided, load from scenario data
            if (!string.IsNullOrEmpty(scenarioId))
            {
                return await _gameScenarioService.LoadScenarioSettingAsync<GameSetting>(scenarioId, "gameSetting.json");
            }
            
            // Otherwise, load from user data
            return await LoadAsync<GameSetting>(userId, "gameSetting");
        }

        // Game Preferences accessors
        public async Task<GamePreferences> GetGamePreferencesAsync(string userId)
        {
            return await LoadAsync<GamePreferences>(userId, "gamePreferences");
        }

        // Location accessors
        public async Task<Location> GetLocationAsync(string userId, string locationId, string scenarioId = null)
        {
            try
            {
                // Check if we need to load from scenario data
                if (!string.IsNullOrEmpty(scenarioId))
                {
                    var scenarioLocation = await _gameScenarioService.LoadScenarioSettingAsync<Location>(scenarioId, $"locations/{locationId}.json");
                    if (scenarioLocation != null)
                    {
                        return scenarioLocation;
                    }
                }
                
                // Try to load from user data if not found in scenario or if no scenarioId provided
                var fileId = $"locations/{locationId}";
                var filePath = GetFilePath(userId, fileId);
                
                if (!File.Exists(filePath))
                {
                    return null;
                }

                var json = await File.ReadAllTextAsync(filePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                
                return System.Text.Json.JsonSerializer.Deserialize<Location>(json, options);
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error loading location {locationId}: {ex.Message}");
                throw;
            }
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
        
        // Method to get all quests in a game
        public async Task<List<Quest>> GetAllQuestsAsync(string gameId)
        {
            try
            {
                var allQuests = new List<Quest>();
                var questsPath = Path.Combine(_dataPath, "userData", gameId, "quests");
                
                if (!Directory.Exists(questsPath))
                {
                    return allQuests;
                }
                
                foreach (var questFile in Directory.GetFiles(questsPath, "*.json"))
                {
                    try
                    {
                        var questJson = await File.ReadAllTextAsync(questFile);
                        var quest = System.Text.Json.JsonSerializer.Deserialize<Quest>(questJson);
                        allQuests.Add(quest);
                    }
                    catch (Exception ex)
                    {
                        _loggingService.LogWarning($"Error reading quest file {questFile}: {ex.Message}");
                    }
                }
                
                return allQuests;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error getting all quests: {ex.Message}");
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

        #endregion

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
    }
} 