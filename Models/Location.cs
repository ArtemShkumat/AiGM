using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AiGMBackEnd.Models
{
    public class Location
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "LOCATION";
        
        [JsonPropertyName("id")]
        public string Id { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("locationType")]
        public string LocationType { get; set; }
        
        [JsonPropertyName("description")]
        public string Description { get; set; }
        
        [JsonPropertyName("knownToPlayer")]
        public bool KnownToPlayer { get; set; }
        
        [JsonPropertyName("connectedLocations")]
        public List<ConnectedLocation> ConnectedLocations { get; set; } = new List<ConnectedLocation>();
        
        [JsonPropertyName("parentLocation")]
        public ParentLocation ParentLocation { get; set; }
        
        [JsonPropertyName("subLocations")]
        public List<SubLocation> SubLocations { get; set; } = new List<SubLocation>();
        
        [JsonPropertyName("npcs")]
        public List<string> Npcs { get; set; } = new List<string>();
        
        [JsonPropertyName("pointsOfInterest")]
        public List<PointOfInterest> PointsOfInterest { get; set; } = new List<PointOfInterest>();
        
        [JsonPropertyName("questIds")]
        public List<string> QuestIds { get; set; } = new List<string>();
        
        [JsonPropertyName("items")]
        public List<string> Items { get; set; } = new List<string>();
    }

    public class ConnectedLocation
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        
        [JsonPropertyName("description")]
        public string Description { get; set; }
    }

    public class ParentLocation
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        
        [JsonPropertyName("description")]
        public string Description { get; set; }
    }

    public class SubLocation
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        
        [JsonPropertyName("description")]
        public string Description { get; set; }
    }

    public class PointOfInterest
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("description")]
        public string Description { get; set; }
    }

    public class HistoryLogEntry
    {
        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; }
        
        [JsonPropertyName("event")]
        public string Event { get; set; }
        
        [JsonPropertyName("npcId")]
        public string NpcId { get; set; }
        
        [JsonPropertyName("description")]
        public string Description { get; set; }
    }
}
