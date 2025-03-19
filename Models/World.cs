using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AiGMBackEnd.Models
{
    public class World
    {
        [JsonPropertyName("gameName")]
        public string GameName { get; set; }
        
        [JsonPropertyName("setting")]
        public string Setting { get; set; }
        
        [JsonPropertyName("gameTime")]
        public string GameTime { get; set; }
        
        [JsonPropertyName("daysSinceStart")]
        public int DaysSinceStart { get; set; }
        
        [JsonPropertyName("currentPlayer")]
        public string CurrentPlayer { get; set; }
        
        [JsonPropertyName("worldStateEffects")]
        public WorldStateEffects WorldStateEffects { get; set; } = new WorldStateEffects();
        
        [JsonPropertyName("lore")]
        public List<LoreSummary> Lore { get; set; } = new List<LoreSummary>();
        
        [JsonPropertyName("locations")]
        public List<LocationSummary> Locations { get; set; } = new List<LocationSummary>();
        
        [JsonPropertyName("npcs")]
        public List<NpcSummary> Npcs { get; set; } = new List<NpcSummary>();
        
        [JsonPropertyName("quests")]
        public List<QuestSummary> Quests { get; set; } = new List<QuestSummary>();
        
        [JsonPropertyName("globalFlags")]
        public List<string> GlobalFlags { get; set; } = new List<string>();
        
        [JsonPropertyName("historyLog")]
        public List<WorldHistoryLogEntry> HistoryLog { get; set; } = new List<WorldHistoryLogEntry>();
        
        [JsonPropertyName("notes")]
        public string Notes { get; set; }
    }

    public class WorldStateEffects
    {
        [JsonPropertyName("weather")]
        public string Weather { get; set; }
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
