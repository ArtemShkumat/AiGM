using System.Threading.Tasks;
using AiGMBackEnd.Models;
using AiGMBackEnd.Models.Locations;
using AiGMBackEnd.Services;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System;

namespace AiGMBackEnd.Services.Processors
{
    public class LocationProcessor : IEntityProcessor
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
            try
            {
                _loggingService.LogInfo("Processing location creation");
                
                // Extract location details
                var locationId = locationData["id"]?.ToString();
                var locationType = locationData["type"]?.ToString();
                
                if (string.IsNullOrEmpty(locationId))
                {
                    _loggingService.LogError("Location ID is missing");
                    return;
                }
                
                // Create a location based on the type
                Location location;
                
                switch (locationType?.ToLower())
                {
                    case "delve":
                        location = ProcessDelve(locationData);
                        break;
                    case "building":
                        location = ProcessBuilding(locationData);
                        break;
                    case "settlement":
                        location = ProcessSettlement(locationData);
                        break;
                    default:
                        _loggingService.LogError($"Unknown location type: {locationType}");
                        return;
                }
                
                // Set common properties
                location.Id = locationId;
                location.Name = locationData["name"]?.ToString() ?? "Unknown Location";
                location.Description = locationData["description"]?.ToString();
                location.KnownToPlayer = locationData["knownToPlayer"]?.Value<bool>() ?? false;
                
                // Process connected locations
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
                
                // Process parent location
                if (locationData["parentLocation"] != null)
                {
                    location.ParentLocation = locationData["parentLocation"]?.ToString();
                }
                
                // Process NPCs
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
                
                // Save the location data
                await _storageService.SaveAsync(userId, $"locations/{locationId}", location);
                
                _loggingService.LogInfo($"Location {locationId} processed and saved successfully");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error processing location creation: {ex.Message}");
                throw;
            }
        }
        
        private Delve ProcessDelve(JObject locationData)
        {
            var delve = new Delve
            {
                Purpose = locationData["purpose"]?.ToString()
            };
            
            // Process entrance room
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
            
            // Process puzzle room
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
            
            // Process setback room
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
            
            // Process climax room
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
            
            // Process reward room
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
            // Process valuables
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
            
            // Process floors
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
                        
                        // Process rooms
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
                                        Description = roomObj["description"]?.ToString(),
                                        Navigation = new RoomNavigation()
                                    };
                                    
                                    // Process points of interest
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
                                    
                                    // Process valuables
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
                                    
                                    // Process NPCs
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
                                    
                                    // Process connected rooms
                                    if (roomObj["navigation"]?["connected_rooms"] is JArray connectedRoomsArray)
                                    {
                                        foreach (var connectedRoom in connectedRoomsArray)
                                        {
                                            var roomStr = connectedRoom.ToString();
                                            if (!string.IsNullOrEmpty(roomStr))
                                            {
                                                room.Navigation.ConnectedRooms.Add(roomStr);
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
            
            // Process districts
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
                        
                        // Process connected districts
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
                        
                        // Process points of interest
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
                        
                        // Process NPCs
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
                        
                        // Process buildings
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