using System.Threading.Tasks;
using AiGMBackEnd.Models;
using AiGMBackEnd.Services;
using Newtonsoft.Json.Linq;

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
                
                if (string.IsNullOrEmpty(locationId))
                {
                    _loggingService.LogError("Location ID is missing");
                    return;
                }
                
                // Create a new Location object based on our model class
                var location = new Models.Location
                {
                    Id = locationId,
                    Name = locationData["name"]?.ToString() ?? "Unknown Location",
                    Type = locationData["type"]?.ToString(),
                    Description = locationData["description"]?.ToString(),
                    KnownToPlayer = locationData["knownToPlayer"]?.Value<bool>() ?? false
                };
                
                // Handle Connected Locations
                if (locationData["connectedLocations"] is JArray connectedLocations)
                {
                    foreach (var conn in connectedLocations)
                    {
                        if (conn is JObject connObj)
                        {
                            location.ConnectedLocations.Add(new Models.ConnectedLocation
                            {
                                Id = connObj["id"]?.ToString(),
                                Description = connObj["description"]?.ToString()
                            });
                        }
                    }
                }
                
                // Handle Parent Location
                if (locationData["parentLocation"] is JObject parentLoc)
                {
                    location.ParentLocation = new Models.ParentLocation
                    {
                        Id = parentLoc["id"]?.ToString(),
                        Description = parentLoc["description"]?.ToString()
                    };
                }
                
                // Handle Sub Locations
                if (locationData["subLocations"] is JArray subLocations)
                {
                    foreach (var sub in subLocations)
                    {
                        if (sub is JObject subObj)
                        {
                            location.SubLocations.Add(new Models.SubLocation
                            {
                                Id = subObj["id"]?.ToString(),
                                Description = subObj["description"]?.ToString()
                            });
                        }
                    }
                }
                
                // Handle NPCs
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
                
                // Handle Points of Interest
                if (locationData["pointsOfInterest"] is JArray pois)
                {
                    foreach (var poi in pois)
                    {
                        if (poi is JObject poiObj)
                        {
                            location.PointsOfInterest.Add(new Models.PointOfInterest
                            {
                                Name = poiObj["name"]?.ToString(),
                                Description = poiObj["description"]?.ToString()
                            });
                        }
                    }
                }
                
                // Handle Quest IDs
                if (locationData["questIds"] is JArray questIds)
                {
                    foreach (var quest in questIds)
                    {
                        var questStr = quest.ToString();
                        if (!string.IsNullOrEmpty(questStr))
                        {
                            location.QuestIds.Add(questStr);
                        }
                    }
                }
                
                // Handle Items
                if (locationData["items"] is JArray items)
                {
                    foreach (var item in items)
                    {
                        var itemStr = item.ToString();
                        if (!string.IsNullOrEmpty(itemStr))
                        {
                            location.Items.Add(itemStr);
                        }
                    }
                }                
                
                // Save the location data
                await _storageService.SaveAsync(userId, $"locations/{locationId}", location);
                
                // Check if there are associated NPCs to create
                if (locationData["npcs"] != null)
                {
                    var npcsArray = locationData["npcs"] as JArray;
                    if (npcsArray != null && npcsArray.Count > 0)
                    {
                        // TODO: Trigger jobs to create missing NPCs if needed
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error processing location creation: {ex.Message}");
                throw;
            }
        }
    }
} 