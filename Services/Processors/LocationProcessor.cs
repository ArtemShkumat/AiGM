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

namespace AiGMBackEnd.Services.Processors
{
    public class LocationProcessor : ILocationProcessor
    {
        private readonly StorageService _storageService;
        private readonly LoggingService _loggingService;
        private readonly GameNotificationService _gameNotificationService;

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
            GameNotificationService gameNotificationService)
        {
            _storageService = storageService;
            _loggingService = loggingService;
            _gameNotificationService = gameNotificationService;
        }

        public async Task ProcessAsync(JObject locationData, string userId)
        {
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

                string locationType = locationData["type"]?.ToString() ?? "LOCATION";
                locationData["type"] = locationType; // Ensure type is set

                // Choose the correct storage method based on whether this is a starting scenario
                if (isStartingScenario)
                {
                    // Get scenarioId from metadata
                    string scenarioId = metadata?["scenarioId"]?.ToString();
                    if (string.IsNullOrEmpty(scenarioId))
                    {
                        _loggingService.LogError($"Missing scenarioId in metadata for starting scenario location {locationId}");
                        return;
                    }

                    string locationPath = Path.Combine("Data", "startingScenarios", scenarioId, "locations", $"{locationId}.json");
                    await File.WriteAllTextAsync(locationPath, locationData.ToString());
                    _loggingService.LogInfo($"Saved starting scenario location {locationId} to {locationPath}");
                }
                else
                {
                    // Normal user save
                    await _storageService.SaveAsync(userId, "location", locationData);
                    _loggingService.LogInfo($"Successfully processed and saved location {locationId} for user {userId}");
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error processing location creation: {ex.Message}");
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

        private async Task ProcessBuildingNestedAsync(JObject locationData, Building building, string userId, List<Task> nestedSaveTasks)
        {
            building.FloorIds = new List<string>();

            if (locationData["floors"] is JArray floorsArray)
            {
                foreach (var floorData in floorsArray)
                {
                    if (floorData is JObject floorObj)
                    {
                        string floorName = floorObj["floor_name"]?.ToString() ?? $"Unnamed_{LocationTypeFloor}";
                        string floorId = GenerateNestedLocationId(building.Id, LocationTypeFloor, floorName);

                        var floorLocation = new GenericLocation
                        {
                            Id = floorId,
                            Name = floorName,
                            LocationType = LocationTypeFloor,
                            ParentLocation = building.Id,
                            Description = floorObj["description"]?.ToString(),
                            Type = "LOCATION"
                        };

                        List<string> roomIdsForThisFloor = new List<string>();
                        if (floorObj["rooms"] is JArray roomsArray)
                        {
                            foreach (var roomData in roomsArray)
                            {
                                if (roomData is JObject roomObj)
                                {
                                    string roomName = roomObj["name"]?.ToString() ?? $"Unnamed_{LocationTypeRoom}";
                                    string roomId = GenerateNestedLocationId(floorId, LocationTypeRoom, roomName);

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
                                    roomIdsForThisFloor.Add(roomId);
                                }
                            }
                        }

                        nestedSaveTasks.Add(_storageService.SaveAsync(userId, $"locations/{floorId}", floorLocation));
                        building.FloorIds.Add(floorId);
                    }
                }
            }
            await Task.CompletedTask;
        }

        private async Task ProcessSettlementNestedAsync(JObject locationData, Settlement settlement, string userId, List<Task> nestedSaveTasks)
        {
            settlement.DistrictIds = new List<string>();

            if (locationData["districts"] is JArray districtsArray)
            {
                foreach (var districtData in districtsArray)
                {
                    if (districtData is JObject districtObj)
                    {
                        string districtName = districtObj["name"]?.ToString() ?? $"Unnamed_{LocationTypeDistrict}";
                        string districtId = GenerateNestedLocationId(settlement.Id, LocationTypeDistrict, districtName);

                        var districtLocation = new GenericLocation
                        {
                            Id = districtId,
                            Name = districtName,
                            LocationType = LocationTypeDistrict,
                            ParentLocation = settlement.Id,
                            Description = districtObj["description"]?.ToString(),
                            TypicalOccupants = districtObj["typicalOccupants"]?.ToString() ?? string.Empty,
                            Type = "LOCATION"
                        };

                        nestedSaveTasks.Add(_storageService.SaveAsync(userId, $"locations/{districtId}", districtLocation));
                        settlement.DistrictIds.Add(districtId);
                    }
                }
            }
            await Task.CompletedTask;
        }

        private async Task ProcessDelveNestedAsync(JObject locationData, Delve delve, string userId, List<Task> nestedSaveTasks)
        {
            delve.DelveRoomIds = new List<string>();

            if (locationData["delve_rooms"] is JArray roomsArray)
            {
                foreach (var roomData in roomsArray)
                {
                    if (roomData is JObject roomObj)
                    {
                        string roomName = roomObj["name"]?.ToString() ?? $"Unnamed_{LocationTypeDelveRoom}";
                        string roomId = GenerateNestedLocationId(delve.Id, LocationTypeDelveRoom, roomName);

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
                            ParentLocation = delve.Id,
                            Description = combinedDescription,
                            Type = "LOCATION"
                        };

                        nestedSaveTasks.Add(_storageService.SaveAsync(userId, $"locations/{roomId}", roomLocation));
                        delve.DelveRoomIds.Add(roomId);
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