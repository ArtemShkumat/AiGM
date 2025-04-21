using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AiGMBackEnd.Models;
using AiGMBackEnd.Services.Storage;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;

namespace AiGMBackEnd.Services.Processors
{
    public class ScenarioProcessor : IScenarioProcessor
    {
        private readonly IGameScenarioService _gameScenarioService;
        private readonly ILogger<ScenarioProcessor> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IStatusTrackingService _statusTrackingService;

        public ScenarioProcessor(
            IGameScenarioService gameScenarioService,
            ILogger<ScenarioProcessor> logger,
            IServiceProvider serviceProvider,
            IStatusTrackingService statusTrackingService)
        {
            _gameScenarioService = gameScenarioService;
            _logger = logger;
            _serviceProvider = serviceProvider;
            _statusTrackingService = statusTrackingService;
        }

        public async Task ProcessAsync(JObject scenarioData, string scenarioId, string userId, bool isStartingScenario = false)
        {
            try
            {
                // Always use true for isStartingScenario since this processor is only for creating starting scenarios
                isStartingScenario = true;
                
                // Create the folder structure
                await _gameScenarioService.CreateScenarioFolderStructureAsync(scenarioId, userId, isStartingScenario);
                
                // Process game settings
                JToken gameSettingData = scenarioData["gameSetting"];
                if (gameSettingData != null)
                {
                    await _gameScenarioService.SaveScenarioFileAsync(scenarioId, "gameSetting.json", gameSettingData, userId, isStartingScenario);
                }
                
                // Create the initial world.json file based on World.cs structure
                JObject worldData = CreateInitialWorldJson(gameSettingData, scenarioId);
                await _gameScenarioService.SaveScenarioFileAsync(scenarioId, "world.json", worldData, userId, isStartingScenario);
                
                // Process locations
                JArray locationsData = (JArray)scenarioData["locations"];
                if (locationsData != null)
                {
                    await ProcessLocationsAsync(scenarioId, locationsData, userId, isStartingScenario);
                }
                
                // Process NPCs
                JArray npcsData = (JArray)scenarioData["npcs"];
                if (npcsData != null)
                {
                    await ProcessNpcsAsync(scenarioId, npcsData, userId, isStartingScenario);
                }
                
                _logger.LogInformation($"Successfully processed scenario {scenarioId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing scenario {scenarioId}: {ex.Message}");
                throw;
            }
        }

        private JObject CreateInitialWorldJson(JToken gameSettingData, string scenarioId)
        {
            string gameTime = gameSettingData?["gameTime"]?.ToString() ?? "Year 1, Day 1, 8:00 AM";
            
            return new JObject
            {
                ["type"] = "WORLD",
                ["gameTime"] = gameTime,
                ["daysSinceStart"] = 1,
                ["currentPlayer"] = "", // Assuming no player initially for scenario
                ["locations"] = new JArray(),
                ["npcs"] = new JArray(),
                ["quests"] = new JArray()
            };
        }

        private async Task ProcessLocationsAsync(string scenarioId, JArray locationsData, string userId, bool isStartingScenario)
        {
            var creationJobs = new Dictionary<string, string>();
            
            foreach (JObject locationObj in locationsData)
            {
                string locationId = locationObj["id"]?.ToString();
                if (string.IsNullOrEmpty(locationId))
                {
                    _logger.LogWarning("Location is missing ID, generating one");
                    locationId = Guid.NewGuid().ToString();
                    locationObj["id"] = locationId;
                }
                
                string locationName = locationObj["name"]?.ToString() ?? "Unnamed Location";
                string locationType = locationObj["locationType"]?.ToString() ?? "Generic";
                string locationDescription = locationObj["description"]?.ToString() ?? "";
                string parentLocationId = locationObj["parentLocationId"]?.ToString();
                
                // Create context for the location creation
                string context = $"Create a detailed {locationType} called '{locationName}'. Description: {locationDescription}";
                
                // Register entity in world first
                await RegisterEntityInWorldAsync(scenarioId, locationId, locationName, "Location", userId, isStartingScenario);
                
                // Schedule the creation job - pass parentLocationId correctly and scenarioId as the last parameter
                string jobId = ScheduleEntityCreation("LOCATION", locationId, locationName, locationType, context, userId, isStartingScenario, parentLocationId, scenarioId);
                if (!string.IsNullOrEmpty(jobId))
                {
                    creationJobs.Add(locationId, jobId);
                    _logger.LogInformation($"Scheduled creation job for location {locationId} ({locationName})");
                }
            }
            
            // Wait for all jobs to be scheduled
            await Task.CompletedTask;
        }
        
        private async Task ProcessNpcsAsync(string scenarioId, JArray npcsData, string userId, bool isStartingScenario)
        {
            var creationJobs = new Dictionary<string, string>();
            
            foreach (JObject npcObj in npcsData)
            {
                string npcId = npcObj["id"]?.ToString();
                if (string.IsNullOrEmpty(npcId))
                {
                    _logger.LogWarning("NPC is missing ID, generating one");
                    npcId = Guid.NewGuid().ToString();
                    npcObj["id"] = npcId;
                }
                
                string npcName = npcObj["name"]?.ToString() ?? "Unnamed NPC";
                string npcDescription = npcObj["description"]?.ToString() ?? "";
                string initialLocationId = npcObj["initialLocationId"]?.ToString();
                
                // Create context for the NPC creation
                string context = $"Create a detailed NPC named '{npcName}'. Description: {npcDescription}";
                
                // Register entity in world first
                await RegisterEntityInWorldAsync(scenarioId, npcId, npcName, "Npc", userId, isStartingScenario);
                
                // Schedule the creation job
                string jobId = ScheduleEntityCreation("NPC", npcId, npcName, null, context, userId, isStartingScenario, initialLocationId, scenarioId);
                if (!string.IsNullOrEmpty(jobId))
                {
                    creationJobs.Add(npcId, jobId);
                    _logger.LogInformation($"Scheduled creation job for NPC {npcId} ({npcName})");
                }
            }
            
            // Wait for all jobs to be scheduled
            await Task.CompletedTask;
        }
        
        private async Task RegisterEntityInWorldAsync(string scenarioId, string entityId, string entityName, string entityType, string userId, bool isStartingScenario)
        {
            try
            {
                // Load world.json
                string worldPath = "world.json";
                var worldJson = await LoadFileAsync(scenarioId, worldPath, userId, isStartingScenario);
                
                if (worldJson == null)
                {
                    _logger.LogError($"World file not found for scenario {scenarioId}");
                    return;
                }
                
                // Add entity to entities array
                var entities = (JArray)worldJson["entities"] ?? new JArray();
                var entityObject = new JObject
                {
                    ["id"] = entityId,
                    ["name"] = entityName,
                    ["type"] = entityType
                };
                
                entities.Add(entityObject);
                worldJson["entities"] = entities;
                
                // Add entity summary based on type
                switch (entityType)
                {
                    case "Location":
                        var locationsArray = (JArray)worldJson["locations"] ?? new JArray();
                        var locationSummary = new JObject
                        {
                            ["id"] = entityId,
                            ["name"] = entityName,
                            // Extract LocationType from the actual location data if available, otherwise default
                            // This requires loading the specific location file which is not done here.
                            // For now, we'll leave it potentially blank or generic. 
                            // A separate WorldSync job might be better for populating details.
                            ["locationType"] = "" // Placeholder - Requires location data lookup
                        };
                        locationsArray.Add(locationSummary);
                        worldJson["locations"] = locationsArray;
                        break;
                    case "Npc":
                        var npcsArray = (JArray)worldJson["npcs"] ?? new JArray();
                        var npcSummary = new JObject
                        {
                            ["id"] = entityId,
                            ["name"] = entityName
                        };
                        npcsArray.Add(npcSummary);
                        worldJson["npcs"] = npcsArray;
                        break;
                    // Add cases for other entity types like Quests if needed
                    default:
                        _logger.LogWarning($"Attempted to register unsupported entity type '{entityType}' in world.json");
                        return; // Don't save if type is unknown
                }

                // Save updated world.json
                await _gameScenarioService.SaveScenarioFileAsync(scenarioId, worldPath, worldJson, userId, isStartingScenario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding entity {entityId} to world: {ex.Message}");
            }
        }
        
        private string ScheduleEntityCreation(string entityType, string entityId, string entityName, string locationType, string context, string userId, bool isStartingScenario, string initialLocationId = null, string scenarioId = null)
        {
            if (string.IsNullOrEmpty(entityId) || string.IsNullOrEmpty(entityName) || string.IsNullOrEmpty(context))
            {
                _logger.LogWarning($"Skipping scheduling for {entityType} due to missing id/name/context.");
                return string.Empty;
            }

            string jobId;

            try
            {
                switch (entityType)
                {
                    case "NPC":
                        // For NPCs, pass the scenarioId directly in the request
                        jobId = BackgroundJob.Enqueue<HangfireJobsService>(service => 
                            service.CreateNpcAsync(
                                userId, 
                                entityId, 
                                entityName, 
                                context, 
                                initialLocationId,
                                isStartingScenario,
                                scenarioId
                            )
                        );
                        _logger.LogInformation($"Scheduled NPC creation job for {entityId}, job ID: {jobId}");
                        break;

                    case "LOCATION":
                        // For Locations, pass the scenarioId directly in the request
                        jobId = BackgroundJob.Enqueue<HangfireJobsService>(service => 
                            service.CreateLocationAsync(
                                userId, 
                                entityId, 
                                entityName, 
                                locationType, 
                                context, 
                                initialLocationId,
                                isStartingScenario,
                                scenarioId
                            )
                        );
                        _logger.LogInformation($"Scheduled location creation job for {entityId}, job ID: {jobId}");
                        break;

                    default:
                        _logger.LogWarning($"Unknown entity type for scheduling: {entityType}");
                        return string.Empty;
                }

                _statusTrackingService.RegisterEntityCreationAsync(userId, entityId, entityType.ToLower());

                return jobId;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error scheduling entity creation job for {entityType} {entityId}: {ex.Message}");
                return string.Empty;
            }
        }
        
        private async Task<JObject> LoadFileAsync(string scenarioId, string filePath, string userId, bool isStartingScenario)
        {
            try
            {
                // Use the GameScenarioService to load the file
                var result = await _gameScenarioService.LoadScenarioSettingAsync<JObject>(scenarioId, filePath);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading file {filePath} for scenario {scenarioId}: {ex.Message}");
                return null;
            }
        }
    }
} 