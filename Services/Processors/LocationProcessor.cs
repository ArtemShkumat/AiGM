using System;
using System.Threading.Tasks;
using AiGMBackEnd.Models;
using AiGMBackEnd.Models.Locations;
using AiGMBackEnd.Services;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using AiGMBackEnd.Services.Storage;

namespace AiGMBackEnd.Services.Processors
{
    public class LocationProcessor : ILocationProcessor
    {
        private readonly StorageService _storageService;
        private readonly LoggingService _loggingService;
        private readonly GameNotificationService _gameNotificationService;
        private readonly IGameScenarioService _gameScenarioService;

        // Define constants for location types used internally
        private const string LocationTypeBuilding = "building";
        private const string LocationTypeDelve = "delve";
        private const string LocationTypeSettlement = "settlement";
        private const string LocationTypeWilds = "wilds";
        private const string LocationTypeFloor = "floor";
        private const string LocationTypeRoom = "room";
        private const string LocationTypeDistrict = "district";
        private const string LocationTypeDelveRoom = "delveroom";

        public LocationProcessor(
            StorageService storageService,
            LoggingService loggingService,
            GameNotificationService gameNotificationService,
            IGameScenarioService gameScenarioService)
        {
            _storageService = storageService;
            _loggingService = loggingService;
            _gameNotificationService = gameNotificationService;
            _gameScenarioService = gameScenarioService;
        }

        public async Task ProcessAsync(JObject locationData, string userId)
        {
            List<Task> nestedSaveTasks = new List<Task>(); // Initialize here for the non-scenario case

            try
            {
                _loggingService.LogInfo("Processing location creation from LLM response.");

                // Check if this is for a starting scenario
                bool isStartingScenario = false;
                var metadata = locationData["metadata"] as JObject;
                if (metadata != null && metadata["isStartingScenario"] != null)
                {
                    bool.TryParse(metadata["isStartingScenario"].ToString(), out isStartingScenario);
                }

                // Extract basic properties
                string locationId = locationData["id"]?.ToString();
                if (string.IsNullOrEmpty(locationId))
                {
                    _loggingService.LogError("Missing locationId in location data");
                    return;
                }

                // Ensure locationType is present in the main JObject for dispatching
                string locationTypeValue = locationData["locationType"]?.ToString();
                 if (string.IsNullOrEmpty(locationTypeValue))
                {
                    _loggingService.LogError($"Missing locationType in location data for {locationId}");
                    return;
                }
                locationData["type"] = "LOCATION"; // Ensure base type is set

                // Determine the specific location type to process nested structures
                Location locationModel = null; // C# model only needed for non-scenario case

                // Process nested structures based on type
                switch (locationTypeValue.ToLower())
                {
                    case LocationTypeBuilding:
                        if (isStartingScenario)
                        {
                            await ProcessBuildingNestedAsync(locationData, null, userId, true, null);
                        }
                        else
                        {
                            Building buildingModel = locationData.ToObject<Building>(JsonSerializer.CreateDefault()); // Deserialize
                            locationModel = buildingModel;
                            await ProcessBuildingNestedAsync(locationData, buildingModel, userId, false, nestedSaveTasks);
                        }
                        break;
                    case LocationTypeSettlement:
                         if (isStartingScenario)
                        {
                            await ProcessSettlementNestedAsync(locationData, null, userId, true, null);
                        }
                        else
                        {
                            Settlement settlementModel = locationData.ToObject<Settlement>(JsonSerializer.CreateDefault()); // Deserialize
                            locationModel = settlementModel;
                            await ProcessSettlementNestedAsync(locationData, settlementModel, userId, false, nestedSaveTasks);
                        }
                        break;
                    case LocationTypeDelve:
                         if (isStartingScenario)
                        {
                            await ProcessDelveNestedAsync(locationData, null, userId, true, null);
                        }
                        else
                        {
                            Delve delveModel = locationData.ToObject<Delve>(JsonSerializer.CreateDefault()); // Deserialize
                            locationModel = delveModel;
                            await ProcessDelveNestedAsync(locationData, delveModel, userId, false, nestedSaveTasks);
                        }
                        break;
                    case LocationTypeWilds:
                        // Wilds only has POIs which don't need separate IDs/files currently
                        if (!isStartingScenario)
                        {
                            Wilds wildsModel = locationData.ToObject<Wilds>(JsonSerializer.CreateDefault()); // Deserialize
                            locationModel = wildsModel;
                            ProcessWildsNested(locationData, wildsModel); // Sync POIs to C# model
                        }
                        // No nested file saving needed for Wilds POIs
                        break;
                    default:
                         _loggingService.LogInfo($"Location type '{locationTypeValue}' for {locationId} has no specific nested structures to process.");
                         if (!isStartingScenario)
                         {
                            // Still need to deserialize to save the C# model if it's a generic type
                            locationModel = locationData.ToObject<GenericLocation>(JsonSerializer.CreateDefault());
                         }
                         break;
                }

                // Perform saving based on scenario flag
                if (isStartingScenario)
                {
                    string scenarioId = metadata?["scenarioId"]?.ToString();
                    if (string.IsNullOrEmpty(scenarioId))
                    {
                        _loggingService.LogError($"Missing scenarioId in metadata for starting scenario location {locationId}");
                        return;
                    }
                    await _gameScenarioService.SaveScenarioLocationAsync(scenarioId, locationId, locationData, userId, isStartingScenario);
                    _loggingService.LogInfo($"Saved starting scenario location {locationId} (with nested IDs) to scenario {scenarioId}");
                }
                else
                {
                    // Wait for any nested location saves to complete
                    if (nestedSaveTasks.Any())
                    {
                         await Task.WhenAll(nestedSaveTasks);
                         _loggingService.LogInfo($"Completed saving {nestedSaveTasks.Count} nested locations for {locationId}.");
                    }
                   
                    // Save the parent C# model (which now contains ID lists for nested items)
                    if (locationModel != null) // Ensure we have a deserialized model
                    {
                         await _storageService.SaveAsync(userId, $"locations/{locationId}", locationModel);
                         _loggingService.LogInfo($"Successfully processed and saved PARENT location {locationId} (C# Model) for user {userId}");
                    }
                    else
                    {
                        _loggingService.LogError($"Failed to save parent location {locationId} for user {userId} because C# model was null.");
                    }
                }
            }
            catch (JsonException jsonEx)
            {
                 _loggingService.LogError($"JSON Error processing location creation: {jsonEx.Message}\nData: {locationData.ToString(Newtonsoft.Json.Formatting.None)}");
                 throw;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error processing location creation: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw;
            }
        }

        private string GenerateNestedLocationId(string parentId, string itemType, string itemName)
        {
            var sanitizedName = Regex.Replace(itemName.ToLower(), @"[^a-z0-9]+", "_").Trim('_');
            const int MaxNameLength = 30;
            if (sanitizedName.Length > MaxNameLength) sanitizedName = sanitizedName.Substring(0, MaxNameLength);
            if (string.IsNullOrWhiteSpace(sanitizedName) || sanitizedName == "_")
            {
                sanitizedName = Guid.NewGuid().ToString("N").Substring(0, 8);
            }

            return $"{parentId}_{itemType}_{sanitizedName}";
        }

        private List<PointOfInterest> ProcessPointsOfInterest(JToken? poisToken)
        {
            var pois = new List<PointOfInterest>();
            if (poisToken is JArray poisArray)
            {
                foreach (var poiData in poisArray)
                {
                    if (poiData is JObject poiObj)
                    {
                        pois.Add(new PointOfInterest
                        {
                            Name = poiObj["name"]?.ToString() ?? "Unnamed POI",
                            Description = poiObj["description"]?.ToString(),
                            HintingAt = poiObj["hinting_at"]?.ToString() ?? string.Empty
                        });
                    }
                }
            }
            return pois;
        }

        private List<Valuable> ProcessValuables(JToken? valuablesToken)
        {
            var valuables = new List<Valuable>();
            if (valuablesToken is JArray valuablesArray)
            {
                foreach (var valuableData in valuablesArray)
                {
                    if (valuableData is JObject valuableObj)
                    {
                        var valuable = new Valuable
                        {
                            Name = valuableObj["name"]?.ToString() ?? "Unnamed Valuable",
                            WhyItsHere = valuableObj["why_its_here"]?.ToString() ?? string.Empty,
                            Description = valuableObj["description"]?.ToString(),
                            Quantity = valuableObj["quantity"]?.Value<int>() ?? 1,
                            Value = valuableObj["value"]?.Value<int>() ?? 0
                        };
                        valuables.Add(valuable);
                    }
                }
            }
            return valuables;
        }

        private async Task ProcessBuildingNestedAsync(JObject locationData, Building building, string userId, bool isStartingScenario, List<Task> nestedSaveTasks)
        {
            // Use parent ID from building model if available (non-scenario), otherwise from JObject
            string parentBuildingId = isStartingScenario ? locationData["id"]?.ToString() : building?.Id;
             if (string.IsNullOrEmpty(parentBuildingId))
            {
                 _loggingService.LogError("Cannot generate nested IDs for building, parent ID is missing.");
                 return;
            }

            // Only initialize C# list if not a starting scenario
            if (!isStartingScenario && building != null)
            {
                building.FloorIds = new List<string>();
            }

            if (locationData["floors"] is JArray floorsArray)
            {
                foreach (var floorData in floorsArray)
                {
                    if (floorData is JObject floorObj)
                    {
                        string floorName = floorObj["floor_name"]?.ToString() ?? $"Unnamed_{LocationTypeFloor}";
                        string floorId = GenerateNestedLocationId(parentBuildingId, LocationTypeFloor, floorName);

                        // Add ID directly to JObject for starting scenarios
                        if (isStartingScenario)
                        {
                            floorObj["id"] = floorId;
                            floorObj["parentLocationId"] = parentBuildingId;
                            floorObj["locationType"] = LocationTypeFloor;
                        }
                        else // Existing logic for live game data
                        {
                            if (building == null) { _loggingService.LogError("Building model is null in non-scenario nested processing."); return; } // Safety check
                            var floorLocation = new GenericLocation
                            {
                                Id = floorId,
                                Name = floorName,
                                LocationType = LocationTypeFloor,
                                ParentLocation = parentBuildingId,
                                Description = floorObj["description"]?.ToString(),
                                Type = "LOCATION"
                            };
                            nestedSaveTasks.Add(_storageService.SaveAsync(userId, $"locations/{floorId}", floorLocation));
                            building.FloorIds.Add(floorId);
                        }

                        // Process rooms recursively, passing the flag
                        await ProcessRoomsNestedAsync(floorObj, floorId, userId, isStartingScenario, nestedSaveTasks);
                    }
                }
            }
            await Task.CompletedTask;
        }

        // Helper method specifically for rooms, called from ProcessBuildingNestedAsync
        private async Task ProcessRoomsNestedAsync(JObject floorObj, string floorId, string userId, bool isStartingScenario, List<Task> nestedSaveTasks)
        {
             if (floorObj["rooms"] is JArray roomsArray)
             {
                 foreach (var roomData in roomsArray)
                 {
                     if (roomData is JObject roomObj)
                     {
                         string roomName = roomObj["name"]?.ToString() ?? $"Unnamed_{LocationTypeRoom}";
                         string roomId = GenerateNestedLocationId(floorId, LocationTypeRoom, roomName);

                         if (isStartingScenario)
                         {
                            roomObj["id"] = roomId;
                            roomObj["parentLocationId"] = floorId;
                            roomObj["locationType"] = LocationTypeRoom;
                         }
                         else
                         {
                             var roomLocation = new GenericLocation
                             {
                                 Id = roomId,
                                 Name = roomName,
                                 LocationType = LocationTypeRoom,
                                 ParentLocation = floorId,
                                 Description = roomObj["description"]?.ToString(),
                                 Type = "LOCATION"
                             };
                             nestedSaveTasks.Add(_storageService.SaveAsync(userId, $"locations/{roomId}", roomLocation));
                             // Note: Rooms are not currently added to Floor.RoomIds, logic assumes direct lookup or exploration
                         }
                     }
                 }
             }
            await Task.CompletedTask;
        }

        // Updated method signature to include isStartingScenario
        private async Task ProcessSettlementNestedAsync(JObject locationData, Settlement settlement, string userId, bool isStartingScenario, List<Task> nestedSaveTasks)
        {
            // Use parent ID from settlement model if available (non-scenario), otherwise from JObject
            string parentSettlementId = isStartingScenario ? locationData["id"]?.ToString() : settlement?.Id;
            if (string.IsNullOrEmpty(parentSettlementId))
            {
                 _loggingService.LogError("Cannot generate nested IDs for settlement, parent ID is missing.");
                 return;
            }

            // Only initialize C# list if not a starting scenario
            if (!isStartingScenario && settlement != null)
            {
                 settlement.DistrictIds = new List<string>();
            }
           
            if (locationData["districts"] is JArray districtsArray)
            {
                foreach (var districtData in districtsArray)
                {
                    if (districtData is JObject districtObj)
                    {
                        string districtName = districtObj["name"]?.ToString() ?? $"Unnamed_{LocationTypeDistrict}";
                        string districtId = GenerateNestedLocationId(parentSettlementId, LocationTypeDistrict, districtName);

                        if (isStartingScenario)
                        {
                            districtObj["id"] = districtId;
                            districtObj["parentLocationId"] = parentSettlementId;
                            districtObj["locationType"] = LocationTypeDistrict;
                        }
                        else // Existing logic for live game data
                        {
                            if (settlement == null) { _loggingService.LogError("Settlement model is null in non-scenario nested processing."); return; } // Safety check
                            var districtLocation = new GenericLocation
                            {
                                Id = districtId,
                                Name = districtName,
                                LocationType = LocationTypeDistrict,
                                ParentLocation = parentSettlementId,
                                Description = districtObj["description"]?.ToString(),
                                TypicalOccupants = districtObj["typicalOccupants"]?.ToString() ?? string.Empty,
                                Type = "LOCATION"
                            };
                            nestedSaveTasks.Add(_storageService.SaveAsync(userId, $"locations/{districtId}", districtLocation));
                            settlement.DistrictIds.Add(districtId);
                        }
                    }
                }
            }
            await Task.CompletedTask;
        }

        // Updated method signature to include isStartingScenario
        private async Task ProcessDelveNestedAsync(JObject locationData, Delve delve, string userId, bool isStartingScenario, List<Task> nestedSaveTasks)
        {
            // Use parent ID from delve model if available (non-scenario), otherwise from JObject
            string parentDelveId = isStartingScenario ? locationData["id"]?.ToString() : delve?.Id;
             if (string.IsNullOrEmpty(parentDelveId))
            {
                 _loggingService.LogError("Cannot generate nested IDs for delve, parent ID is missing.");
                 return;
            }

            // Only initialize C# list if not a starting scenario
            if (!isStartingScenario && delve != null)
            {
                delve.DelveRoomIds = new List<string>();
            }

            if (locationData["delve_rooms"] is JArray roomsArray)
            {
                foreach (var roomData in roomsArray)
                {
                    if (roomData is JObject roomObj)
                    {
                        string roomName = roomObj["name"]?.ToString() ?? $"Unnamed_{LocationTypeDelveRoom}";
                        string roomId = GenerateNestedLocationId(parentDelveId, LocationTypeDelveRoom, roomName);

                        if (isStartingScenario)
                        {
                            string role = roomObj["role"]?.ToString() ?? "Unknown Role";
                            string challenge = roomObj["challenge"]?.ToString() ?? "No specific challenge described.";
                            string baseDescription = roomObj["description"]?.ToString() ?? string.Empty;
                            string combinedDescription = $@"Role: {role}
Challenge: {challenge}

{baseDescription}";

                            roomObj["id"] = roomId;
                            roomObj["parentLocationId"] = parentDelveId;
                            roomObj["description"] = combinedDescription; // Update description in JObject too
                            roomObj["locationType"] = LocationTypeDelveRoom;
                        }
                        else // Existing logic for live game data
                        {
                             if (delve == null) { _loggingService.LogError("Delve model is null in non-scenario nested processing."); return; } // Safety check
                             string role = roomObj["role"]?.ToString() ?? "Unknown Role";
                            string challenge = roomObj["challenge"]?.ToString() ?? "No specific challenge described.";
                            string baseDescription = roomObj["description"]?.ToString() ?? string.Empty;
                            string combinedDescription = $@"Role: {role}
Challenge: {challenge}

{baseDescription}";

                            var roomLocation = new GenericLocation
                            {
                                Id = roomId,
                                Name = roomName,
                                LocationType = LocationTypeDelveRoom,
                                ParentLocation = parentDelveId,
                                Description = combinedDescription,
                                Type = "LOCATION"
                            };
                            nestedSaveTasks.Add(_storageService.SaveAsync(userId, $"locations/{roomId}", roomLocation));
                            delve.DelveRoomIds.Add(roomId);
                        }
                    }
                }
            }
            await Task.CompletedTask;
        }

        private void ProcessWildsNested(JObject locationData, Wilds wilds)
        {
            wilds.PointsOfInterest = ProcessPointsOfInterest(locationData["points_of_interest"]);
        }
    }
} 