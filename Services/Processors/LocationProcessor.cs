using System;
using System.Threading.Tasks;
using AiGMBackEnd.Models;
using AiGMBackEnd.Models.Locations;
using AiGMBackEnd.Services;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace AiGMBackEnd.Services.Processors
{
    public class LocationProcessor : ILocationProcessor
    {
        private readonly StorageService _storageService;
        private readonly LoggingService _loggingService;

        public LocationProcessor(
            StorageService storageService,
            LoggingService loggingService)
        {
            _storageService = storageService;
            _loggingService = loggingService;
        }

        public async Task ProcessAsync(JObject locationData, string userId)
        {
            string? locationId = locationData["id"]?.ToString();
            string? locationType = locationData["locationType"]?.ToString();

            try
            {
                _loggingService.LogInfo($"Processing location creation (Type: {locationType ?? "Unknown"}) for ID: {locationId ?? "Unknown"} using direct deserialization.");

                // Instead of trying to deserialize to abstract Location class, use type-specific deserialization
                Location location;
                
                switch (locationType?.ToLower())
                {
                    case "building":
                        location = ProcessBuilding(locationData);
                        break;
                    case "delve":
                        location = ProcessDelve(locationData);
                        break;
                    case "settlement":
                        location = ProcessSettlement(locationData);
                        break;
                    case "wilds":
                        location = ProcessWilds(locationData);
                        break;
                    default:
                        _loggingService.LogError($"Unknown or unsupported location type: {locationType}");
                        throw new JsonException($"Unknown or unsupported location type: {locationType}");
                }

                // Set common properties immediately after type-specific deserialization
                if (location != null)
                {
                    location.LocationType = locationType;
                    location.Id = locationId;
                    location.Name = locationData["name"]?.ToString() ?? "Unknown Location";
                    location.Description = locationData["description"]?.ToString();
                    location.KnownToPlayer = locationData["knownToPlayer"]?.Value<bool>() ?? false;
                    
                    if (string.IsNullOrEmpty(location.Type) || location.Type != "LOCATION")
                    {
                        _loggingService.LogWarning($"Location base type mismatch or missing for {location.Id}. Setting to 'LOCATION'.");
                        location.Type = "LOCATION";
                    }
                }

                if (location == null || string.IsNullOrEmpty(location.Id) || string.IsNullOrEmpty(location.LocationType))
                {
                    _loggingService.LogError("Failed to create location data, or ID/LocationType is missing.");
                    _loggingService.LogWarning($"Attempted location creation for ID: {locationId ?? "Not Found"}, Type: {locationType ?? "Not Found"}");
                    return;
                }
                
                if (locationData["connectedLocations"] is JArray connectedLocations)
                {
                    foreach (var conn in connectedLocations)
                    {
                        var connStr = conn.ToString();
                        if (!string.IsNullOrEmpty(connStr))
                        {
                            location.ConnectedLocations.Add(connStr);
                        }
                    }
                }
                
                if (locationData["parentLocation"] != null)
                {
                    location.ParentLocation = locationData["parentLocation"]?.ToString();
                }
                
                if (locationData["npcs"] is JArray npcs)
                {
                    foreach (var npc in npcs)
                    {
                        var npcStr = npc.ToString();
                        if (!string.IsNullOrEmpty(npcStr))
                        {
                            location.Npcs.Add(npcStr);
                        }
                    }
                }
                
                await _storageService.SaveAsync(userId, $"locations/{location.Id}", location);
                _loggingService.LogInfo($"Successfully processed and saved location: {location.Id} (Type: {location.LocationType})");
            }
            catch (JsonSerializationException jsonEx)
            {
                _loggingService.LogError($"JSON deserialization error processing location ({locationType ?? "Unknown"}) creation for ID {locationId ?? "Unknown"}: {jsonEx.Message}");
                _loggingService.LogInfo($"Problematic JSON data: {locationData.ToString()}");
                throw;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error processing location ({locationType ?? "Unknown"}) creation for ID {locationId ?? "Unknown"}: {ex.Message}");
                _loggingService.LogInfo($"JSON data during error: {locationData.ToString()}");
                throw;
            }
        }
        
        private Wilds ProcessWilds(JObject locationData)
        {
            var wilds = new Wilds
            {
                Terrain = locationData["terrain"]?.ToString(),
                Dangers = locationData["dangers"]?.ToString()
            };
            
            if (locationData["points_of_interest"] is JArray poisArray)
            {
                foreach (var poiData in poisArray)
                {
                    if (poiData is JObject poiObj)
                    {
                        var poi = new PointOfInterest
                        {
                            Name = poiObj["name"]?.ToString(),
                            Description = poiObj["description"]?.ToString(),
                            HintingAt = poiObj["hinting_at"]?.ToString()
                        };
                        
                        wilds.PointsOfInterest.Add(poi);
                    }
                }
            }
            
            return wilds;
        }
        
        private Delve ProcessDelve(JObject locationData)
        {
            var delve = new Delve
            {
                Purpose = locationData["purpose"]?.ToString()
            };
            
            if (locationData["entrance_room"] is JObject entranceRoomObj)
            {
                delve.EntranceRoom = new EntranceRoom
                {
                    RoomNumber = entranceRoomObj["room_number"]?.Value<int>() ?? 1,
                    Role = entranceRoomObj["role"]?.ToString() ?? "Entrance",
                    Name = entranceRoomObj["name"]?.ToString(),
                    Description = entranceRoomObj["description"]?.ToString(),
                    HazardOrGuardian = entranceRoomObj["hazard_or_guardian"]?.ToString()
                };
                
                ProcessRoomValuables(entranceRoomObj, delve.EntranceRoom.Valuables);
            }
            
            if (locationData["puzzle_room"] is JObject puzzleRoomObj)
            {
                delve.PuzzleRoom = new PuzzleRoom
                {
                    RoomNumber = puzzleRoomObj["room_number"]?.Value<int>() ?? 2,
                    Role = puzzleRoomObj["role"]?.ToString() ?? "Puzzle",
                    Name = puzzleRoomObj["name"]?.ToString(),
                    Description = puzzleRoomObj["description"]?.ToString(),
                    PuzzleOrRoleplayChallenge = puzzleRoomObj["puzzle_or_roleplay_challenge"]?.ToString()
                };
                
                ProcessRoomValuables(puzzleRoomObj, delve.PuzzleRoom.Valuables);
            }
            
            if (locationData["setback_room"] is JObject setbackRoomObj)
            {
                delve.SetbackRoom = new SetbackRoom
                {
                    RoomNumber = setbackRoomObj["room_number"]?.Value<int>() ?? 3,
                    Role = setbackRoomObj["role"]?.ToString() ?? "Setback",
                    Name = setbackRoomObj["name"]?.ToString(),
                    Description = setbackRoomObj["description"]?.ToString(),
                    TrickOrSetback = setbackRoomObj["trick_or_setback"]?.ToString()
                };
                
                ProcessRoomValuables(setbackRoomObj, delve.SetbackRoom.Valuables);
            }
            
            if (locationData["climax_room"] is JObject climaxRoomObj)
            {
                delve.ClimaxRoom = new ClimaxRoom
                {
                    RoomNumber = climaxRoomObj["room_number"]?.Value<int>() ?? 4,
                    Role = climaxRoomObj["role"]?.ToString() ?? "Climax",
                    Name = climaxRoomObj["name"]?.ToString(),
                    Description = climaxRoomObj["description"]?.ToString(),
                    ClimaxConflict = climaxRoomObj["climax_conflict"]?.ToString(),
                    HazardOrGuardian = climaxRoomObj["hazard_or_guardian"]?.ToString()
                };
                
                ProcessRoomValuables(climaxRoomObj, delve.ClimaxRoom.Valuables);
            }
            
            if (locationData["reward_room"] is JObject rewardRoomObj)
            {
                delve.RewardRoom = new RewardRoom
                {
                    RoomNumber = rewardRoomObj["room_number"]?.Value<int>() ?? 5,
                    Role = rewardRoomObj["role"]?.ToString() ?? "Reward",
                    Name = rewardRoomObj["name"]?.ToString(),
                    Description = rewardRoomObj["description"]?.ToString(),
                    RewardOrRevelation = rewardRoomObj["reward_or_revelation"]?.ToString()
                };
                
                ProcessRoomValuables(rewardRoomObj, delve.RewardRoom.Valuables);
            }
            
            return delve;
        }
        
        private void ProcessRoomValuables(JObject roomObj, List<DelveValuable> valuablesList)
        {
            if (roomObj["valuables"] is JArray valuablesArray)
            {
                foreach (var valuableData in valuablesArray)
                {
                    if (valuableData is JObject valuableObj)
                    {
                        var valuable = new DelveValuable
                        {
                            Name = valuableObj["name"]?.ToString(),
                            WhyItsHere = valuableObj["why_its_here"]?.ToString(),
                            Description = valuableObj["description"]?.ToString(),
                            Quantity = valuableObj["quantity"]?.Value<int>() ?? 1,
                            Value = valuableObj["value"]?.Value<int>() ?? 0,
                            WhereExactly = valuableObj["where_exactly"]?.ToString()
                        };
                        
                        valuablesList.Add(valuable);
                    }
                }
            }
        }
        
        private Building ProcessBuilding(JObject locationData)
        {
            var building = new Building
            {
                Purpose = locationData["purpose"]?.ToString(),
                History = locationData["history"]?.ToString(),
                ExteriorDescription = locationData["exterior_description"]?.ToString()
            };
            
            if (locationData["floors"] is JArray floorsArray)
            {
                foreach (var floorData in floorsArray)
                {
                    if (floorData is JObject floorObj)
                    {
                        var floor = new Floor
                        {
                            FloorName = floorObj["floor_name"]?.ToString(),
                            Description = floorObj["description"]?.ToString()
                        };
                        
                        if (floorObj["rooms"] is JArray roomsArray)
                        {
                            foreach (var roomData in roomsArray)
                            {
                                if (roomData is JObject roomObj)
                                {
                                    var room = new Room
                                    {
                                        Name = roomObj["name"]?.ToString(),
                                        Type = roomObj["type"]?.ToString(),
                                        Description = roomObj["description"]?.ToString()
                                    };
                                    
                                    if (roomObj["points_of_interest"] is JArray poisArray)
                                    {
                                        foreach (var poiData in poisArray)
                                        {
                                            if (poiData is JObject poiObj)
                                            {
                                                var poi = new PointOfInterest
                                                {
                                                    Name = poiObj["name"]?.ToString(),
                                                    Description = poiObj["description"]?.ToString(),
                                                    HintingAt = poiObj["hinting_at"]?.ToString()
                                                };
                                                
                                                room.PointsOfInterest.Add(poi);
                                            }
                                        }
                                    }
                                    
                                    if (roomObj["valuables"] is JArray valuablesArray)
                                    {
                                        foreach (var valuableData in valuablesArray)
                                        {
                                            if (valuableData is JObject valuableObj)
                                            {
                                                var valuable = new Valuable
                                                {
                                                    Name = valuableObj["name"]?.ToString(),
                                                    Description = valuableObj["description"]?.ToString()
                                                };
                                                
                                                room.Valuables.Add(valuable);
                                            }
                                        }
                                    }
                                    
                                    if (roomObj["npcs"] is JArray npcsArray)
                                    {
                                        foreach (var npc in npcsArray)
                                        {
                                            var npcStr = npc.ToString();
                                            if (!string.IsNullOrEmpty(npcStr))
                                            {
                                                room.Npcs.Add(npcStr);
                                            }
                                        }
                                    }
                                    
                                    if (roomObj["connected_rooms"] is JArray connectedRoomsArray)
                                    {
                                        foreach (var connectedRoom in connectedRoomsArray)
                                        {
                                            var roomStr = connectedRoom.ToString();
                                            if (!string.IsNullOrEmpty(roomStr))
                                            {
                                                room.ConnectedRooms.Add(roomStr);
                                            }
                                        }
                                    }
                                    
                                    floor.Rooms.Add(room);
                                }
                            }
                        }
                        
                        building.Floors.Add(floor);
                    }
                }
            }
            
            return building;
        }
        
        private Settlement ProcessSettlement(JObject locationData)
        {
            var settlement = new Settlement
            {
                Purpose = locationData["purpose"]?.ToString(),
                History = locationData["history"]?.ToString(),
                Size = locationData["size"]?.ToString(),
                Population = locationData["population"]?.Value<int>() ?? 0
            };
            
            if (locationData["districts"] is JArray districtsArray)
            {
                foreach (var districtData in districtsArray)
                {
                    if (districtData is JObject districtObj)
                    {
                        var district = new District
                        {
                            Name = districtObj["name"]?.ToString(),
                            Description = districtObj["description"]?.ToString()
                        };
                        
                        if (districtObj["connected_districts"] is JArray connectedDistrictsArray)
                        {
                            foreach (var connectedDistrict in connectedDistrictsArray)
                            {
                                var districtStr = connectedDistrict.ToString();
                                if (!string.IsNullOrEmpty(districtStr))
                                {
                                    district.ConnectedDistricts.Add(districtStr);
                                }
                            }
                        }
                        
                        if (districtObj["points_of_interest"] is JArray poisArray)
                        {
                            foreach (var poiData in poisArray)
                            {
                                if (poiData is JObject poiObj)
                                {
                                    var poi = new PointOfInterest
                                    {
                                        Name = poiObj["name"]?.ToString(),
                                        Description = poiObj["description"]?.ToString(),
                                        HintingAt = poiObj["hinting_at"]?.ToString()
                                    };
                                    
                                    district.PointsOfInterest.Add(poi);
                                }
                            }
                        }
                        
                        if (districtObj["npcs"] is JArray npcsArray)
                        {
                            foreach (var npc in npcsArray)
                            {
                                var npcStr = npc.ToString();
                                if (!string.IsNullOrEmpty(npcStr))
                                {
                                    district.Npcs.Add(npcStr);
                                }
                            }
                        }
                        
                        if (districtObj["buildings"] is JArray buildingsArray)
                        {
                            foreach (var building in buildingsArray)
                            {
                                var buildingStr = building.ToString();
                                if (!string.IsNullOrEmpty(buildingStr))
                                {
                                    district.Buildings.Add(buildingStr);
                                }
                            }
                        }
                        
                        settlement.Districts.Add(district);
                    }
                }
            }
            
            return settlement;
        }
    }
} 