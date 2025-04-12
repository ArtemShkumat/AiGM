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
        /// This method scans the NPCs, locations, quests, and lore folders and updates the world.json file accordingly.
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
                var lore = new List<LoreSummary>();

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

                // Scan Lore
                string lorePath = Path.Combine(userDataPath, "lore");
                if (Directory.Exists(lorePath))
                {
                    foreach (var loreFile in Directory.GetFiles(lorePath, "*.json"))
                    {
                        try
                        {
                            var loreJson = await File.ReadAllTextAsync(loreFile);
                            // Creating a dynamic object since we don't have access to the Lore class directly
                            var loreObj = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(loreJson);
                            
                            // Extract the required properties
                            string id = loreObj.GetProperty("id").GetString();
                            string title = loreObj.GetProperty("title").GetString();
                            string summary = loreObj.GetProperty("summary").GetString();
                            
                            lore.Add(new LoreSummary 
                            { 
                                Id = id, 
                                Title = title,
                                Summary = summary
                            });
                        }
                        catch (Exception ex)
                        {
                            _loggingService.LogWarning($"Error reading lore file {loreFile}: {ex.Message}");
                        }
                    }
                }

                // Update the world with new entity lists
                world.Npcs = npcs;
                world.Locations = locations;
                world.Quests = quests;
                world.Lore = lore;

                // Save the updated world file
                await _baseStorageService.SaveAsync(userId, "world", world);
                
                _loggingService.LogInfo($"Successfully synchronized world.json for user {userId} with {npcs.Count} NPCs, {locations.Count} locations, {quests.Count} quests, and {lore.Count} lore entries");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error synchronizing world with entities: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Synchronizes NPCs with their locations to ensure location.Npcs arrays contain the NPCs that reference them
        /// </summary>
        /// <param name="gameId">The game/user ID</param>
        /// <returns>Detailed results of the synchronization process</returns>
        public async Task<(int UpdatedCount, List<object> SyncResults)> SyncNpcLocationsAsync(string gameId)
        {
            try
            {
                _loggingService.LogInfo($"Starting NPC location synchronization for game: {gameId}");
                
                // Get all NPCs in the game
                var allNpcs = await _entityStorageService.GetAllNpcsAsync(gameId);
                
                if (allNpcs == null || !allNpcs.Any())
                {
                    return (0, new List<object> { new { Message = "No NPCs found to synchronize." } });
                }
                
                var syncResults = new List<object>();
                int updatedCount = 0;
                
                // Process each NPC
                foreach (var npc in allNpcs)
                {
                    if (string.IsNullOrEmpty(npc.CurrentLocationId))
                    {
                        syncResults.Add(new { 
                            NpcId = npc.Id, 
                            NpcName = npc.Name, 
                            Status = "Skipped", 
                            Reason = "No current location set" 
                        });
                        continue;
                    }
                    
                    // Get the location
                    var location = await _entityStorageService.GetLocationAsync(gameId, npc.CurrentLocationId);
                    
                    if (location == null)
                    {
                        syncResults.Add(new { 
                            NpcId = npc.Id, 
                            NpcName = npc.Name, 
                            Status = "Error", 
                            Reason = $"Location {npc.CurrentLocationId} not found" 
                        });
                        continue;
                    }
                    
                    // Initialize NPCs list if it's null
                    if (location.Npcs == null)
                    {
                        location.Npcs = new List<string>();
                    }
                    
                    // Check if the NPC is already in the location's NPCs list
                    if (!location.Npcs.Contains(npc.Id))
                    {
                        // Add the NPC to the location
                        location.Npcs.Add(npc.Id);
                        
                        // Save the updated location
                        await _baseStorageService.SaveAsync(gameId, $"locations/{location.Id}", location);
                        
                        updatedCount++;
                        syncResults.Add(new { 
                            NpcId = npc.Id, 
                            NpcName = npc.Name, 
                            LocationId = location.Id, 
                            LocationName = location.Name, 
                            Status = "Updated", 
                            Action = "Added NPC to location" 
                        });
                    }
                    else
                    {
                        syncResults.Add(new { 
                            NpcId = npc.Id, 
                            NpcName = npc.Name, 
                            LocationId = location.Id, 
                            LocationName = location.Name, 
                            Status = "Skipped", 
                            Reason = "NPC already in location list" 
                        });
                    }
                }
                
                _loggingService.LogInfo($"NPC location synchronization complete for game {gameId}. Updated {updatedCount} locations.");
                return (updatedCount, syncResults);
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error during NPC location synchronization for game {gameId}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Updates all NPCs in a specific location to set their VisibleToPlayer property to false
        /// This is typically called after a player leaves a location and a conversation is summarized
        /// </summary>
        /// <param name="userId">The user/game ID</param>
        /// <param name="locationId">The location ID to process</param>
        /// <returns>The number of NPCs that were updated</returns>
        public async Task<int> HideNpcsInLocationAsync(string userId, string locationId)
        {
            try
            {
                _loggingService.LogInfo($"Hiding NPCs in location {locationId} for user {userId}");
                
                // Get all NPCs in the specified location
                var npcsInLocation = await _entityStorageService.GetNpcsInLocationAsync(userId, locationId);
                
                if (npcsInLocation == null || !npcsInLocation.Any())
                {
                    _loggingService.LogInfo($"No NPCs found in location {locationId}");
                    return 0;
                }
                
                int updatedCount = 0;
                
                // Process each NPC in the location
                foreach (var npc in npcsInLocation)
                {
                    if (npc.VisibleToPlayer)
                    {
                        // Update the NPC's visibility
                        npc.VisibleToPlayer = false;
                        
                        // Save the updated NPC
                        await _baseStorageService.SaveAsync(userId, $"npcs/{npc.Id}", npc);
                        
                        updatedCount++;
                        _loggingService.LogInfo($"Set NPC {npc.Id} ({npc.Name}) to not visible to player");
                    }
                }
                
                _loggingService.LogInfo($"Updated visibility for {updatedCount} NPCs in location {locationId}");
                return updatedCount;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error updating NPC visibility in location {locationId}: {ex.Message}");
                throw;
            }
        }
    }
} 