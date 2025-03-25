using System.Collections.Generic;
using System.Text.Json.Serialization;
using AiGMBackEnd.Models.Locations;

namespace AiGMBackEnd.Models
{
    public class Building : Location
    {
        public Building()
        {
            Type = "Building";
        }

        [JsonPropertyName("exterior_description")]
        public string ExteriorDescription { get; set; }

        [JsonPropertyName("purpose")]
        public string Purpose { get; set; }

        [JsonPropertyName("history")]
        public string History { get; set; }

        [JsonPropertyName("floors")]
        public List<Floor> Floors { get; set; } = new List<Floor>();
    }

    public class Floor
    {
        [JsonPropertyName("floor_name")]
        public string FloorName { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("rooms")]
        public List<Room> Rooms { get; set; } = new List<Room>();
    }

    public class Room
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("points_of_interest")]
        public List<PointOfInterest> PointsOfInterest { get; set; } = new List<PointOfInterest>();

        [JsonPropertyName("valuables")]
        public List<Valuable> Valuables { get; set; } = new List<Valuable>();

        [JsonPropertyName("npcs")]
        public List<string> Npcs { get; set; } = new List<string>();

        [JsonPropertyName("navigation")]
        public RoomNavigation Navigation { get; set; }
    }

    public class RoomNavigation
    {
        [JsonPropertyName("connected_rooms")]
        public List<string> ConnectedRooms { get; set; } = new List<string>();
    }
} 