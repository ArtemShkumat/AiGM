using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AiGMBackEnd.Models
{
    public class World
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "WORLD";       
        
        [JsonPropertyName("gameTime")]
        public string GameTime { get; set; }
        
        [JsonPropertyName("daysSinceStart")]
        public int DaysSinceStart { get; set; }
        
        [JsonPropertyName("currentPlayer")]
        public string CurrentPlayer { get; set; }
        
        [JsonPropertyName("worldStateEffects")]
        public Dictionary<string, string> WorldStateEffects { get; set; } = new Dictionary<string, string>();
        
        [JsonPropertyName("lore")]
        public List<LoreSummary> Lore { get; set; } = new List<LoreSummary>();
        
        [JsonPropertyName("locations")]
        public List<LocationSummary> Locations { get; set; } = new List<LocationSummary>();
        
        [JsonPropertyName("npcs")]
        public List<NpcSummary> Npcs { get; set; } = new List<NpcSummary>();
        
        [JsonPropertyName("quests")]
        public List<QuestSummary> Quests { get; set; } = new List<QuestSummary>();
    }

    public class LoreSummary
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        
        [JsonPropertyName("title")]
        public string Title { get; set; }
        
        [JsonPropertyName("summary")]
        public string Summary { get; set; }
    }

    public class LocationSummary
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public class NpcSummary
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public class QuestSummary
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        
        [JsonPropertyName("title")]
        public string Title { get; set; }
    }

    public class WorldHistoryLogEntry
    {
        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; }
        
        [JsonPropertyName("event")]
        public string Event { get; set; }
        
        [JsonPropertyName("description")]
        public string Description { get; set; }
    }
}
