using System.Text.Json.Serialization;

namespace AiGMBackEnd.Models
{
    // Base interface for creation hooks
    public interface ICreationHook
    {
        [JsonPropertyName("type")]
        string Type { get; } // Readonly after deserialization
        [JsonPropertyName("id")]
        string Id { get; set; }
        [JsonPropertyName("name")]
        string Name { get; set; }
        [JsonPropertyName("context")]
        string Context { get; set; }
    }

    public class NpcCreationHook : ICreationHook
    {
        [JsonPropertyName("type")]
        public string Type => "NPC";
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("context")]
        public string Context { get; set; }
        [JsonPropertyName("currentLocationId")]
        public string CurrentLocationId { get; set; } // Specific to NPC
    }

    public class LocationCreationHook : ICreationHook
    {
        [JsonPropertyName("type")]
        public string Type => "LOCATION";
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("context")]
        public string Context { get; set; }
        [JsonPropertyName("locationType")]
        public string LocationType { get; set; } // Specific to Location (Building, Settlement, Delve, Wilds)
    }

    public class QuestCreationHook : ICreationHook
    {
        [JsonPropertyName("type")]
        public string Type => "QUEST";
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("context")]
        public string Context { get; set; }
    }
    
    public class EnemyStatBlockCreationHook : ICreationHook
    {
        [JsonPropertyName("type")]
        public string Type => "ENEMY_STAT_BLOCK";
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("context")]
        public string Context { get; set; }
        
        [JsonPropertyName("level")]
        public int Level { get; set; } = 1;
        
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
        
        [JsonPropertyName("vulnerability")]
        public string Vulnerability { get; set; } = string.Empty;
        
        [JsonPropertyName("badStuff")]
        public string BadStuff { get; set; } = string.Empty;
        
        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new List<string>();
    }
} 