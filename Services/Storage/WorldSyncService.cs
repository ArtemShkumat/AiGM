using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AiGMBackEnd.Models;
using AiGMBackEnd.Models.Locations;

namespace AiGMBackEnd.Services.Storage
{
    public class WorldSyncService : IWorldSyncService
    {
        private readonly LoggingService _loggingService;
        private readonly IBaseStorageService _baseStorageService;
        private readonly IEntityStorageService _entityStorageService;
        private readonly string _dataPath;

        public WorldSyncService(LoggingService loggingService, IBaseStorageService baseStorageService, IEntityStorageService entityStorageService)
        {
            _loggingService = loggingService;
            _baseStorageService = baseStorageService;
            _entityStorageService = entityStorageService;
            
            // Set the data path
            string rootDirectory = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.FullName;
            _dataPath = Path.Combine(rootDirectory, "Data");
        }

        /// <summary>
        /// Synchronizes the world.json file with all existing entities in the game directory structure.
        /// This method scans the NPCs, locations, quests folders and updates the world.json file accordingly.
        /// </summary>
        /// <param name="userId">The user/game ID</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task SyncWorldWithEntitiesAsync(string userId)
        {
            try
            {
                // Load the current world data
                var world = await _baseStorageService.LoadAsync<World>(userId, "world");
                if (world == null)
                {
                    _loggingService.LogError($"Cannot sync world for user {userId}: world.json not found");
                    return;
                }

                // Get the userData directory path
                string userDataPath = Path.Combine(_dataPath, "userData", userId);
                
                // Initialize empty lists for each entity type
                var npcs = new List<NpcSummary>();
                var locations = new List<LocationSummary>();
                var quests = new List<QuestSummary>();

                // Scan NPCs
                string npcsPath = Path.Combine(userDataPath, "npcs");
                if (Directory.Exists(npcsPath))
                {
                    foreach (var npcFile in Directory.GetFiles(npcsPath, "*.json"))
                    {
                        try
                        {
                            var npcJson = await File.ReadAllTextAsync(npcFile);
                            var npc = System.Text.Json.JsonSerializer.Deserialize<Npc>(npcJson);
                            npcs.Add(new NpcSummary { Id = npc.Id, Name = npc.Name });
                        }
                        catch (Exception ex)
                        {
                            _loggingService.LogWarning($"Error reading NPC file {npcFile}: {ex.Message}");
                        }
                    }
                }

                // Scan Locations
                string locationsPath = Path.Combine(userDataPath, "locations");
                if (Directory.Exists(locationsPath))
                {
                    foreach (var locationFile in Directory.GetFiles(locationsPath, "*.json"))
                    {
                        try
                        {
                            var locationJson = await File.ReadAllTextAsync(locationFile);
                            var location = System.Text.Json.JsonSerializer.Deserialize<Location>(locationJson);
                            locations.Add(new LocationSummary 
                            { 
                                Id = location.Id, 
                                Name = location.Name,
                                LocationType = location.LocationType
                            });
                        }
                        catch (Exception ex)
                        {
                            _loggingService.LogWarning($"Error reading location file {locationFile}: {ex.Message}");
                        }
                    }
                }

                // Scan Quests
                string questsPath = Path.Combine(userDataPath, "quests");
                if (Directory.Exists(questsPath))
                {
                    foreach (var questFile in Directory.GetFiles(questsPath, "*.json"))
                    {
                        try
                        {
                            var questJson = await File.ReadAllTextAsync(questFile);
                            var quest = System.Text.Json.JsonSerializer.Deserialize<Quest>(questJson);
                            quests.Add(new QuestSummary { Id = quest.Id, Title = quest.Title });
                        }
                        catch (Exception ex)
                        {
                            _loggingService.LogWarning($"Error reading quest file {questFile}: {ex.Message}");
                        }
                    }
                }

                

                // Update the world with new entity lists
                world.Npcs = npcs;
                world.Locations = locations;
                world.Quests = quests;

                // Save the updated world file
                await _baseStorageService.SaveAsync(userId, "world", world);
                
                _loggingService.LogInfo($"Successfully synchronized world.json for user {userId} with {npcs.Count} NPCs, {locations.Count} locations, {quests.Count} quests");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error synchronizing world with entities: {ex.Message}");
                throw;
            }
        }        
    }
}