using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AiGMBackEnd.Models
{
    public class Lore
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        
        [JsonPropertyName("title")]
        public string Title { get; set; }
        
        [JsonPropertyName("summary")]
        public string Summary { get; set; }
        
        [JsonPropertyName("fullDescription")]
        public string FullDescription { get; set; }
        
        [JsonPropertyName("relevance")]
        public LoreRelevance Relevance { get; set; } = new LoreRelevance();
        
        [JsonPropertyName("rumors")]
        public List<string> Rumors { get; set; } = new List<string>();
        
        [JsonPropertyName("historicalEvents")]
        public List<HistoricalEvent> HistoricalEvents { get; set; } = new List<HistoricalEvent>();
        
        [JsonPropertyName("notes")]
        public string Notes { get; set; }
    }

    public class LoreRelevance
    {
        [JsonPropertyName("currentImportance")]
        public string CurrentImportance { get; set; }
        
        [JsonPropertyName("associatedQuests")]
        public List<string> AssociatedQuests { get; set; } = new List<string>();
        
        [JsonPropertyName("associatedNpcs")]
        public List<string> AssociatedNpcs { get; set; } = new List<string>();
        
        [JsonPropertyName("associatedLocations")]
        public List<string> AssociatedLocations { get; set; } = new List<string>();
    }

    public class HistoricalEvent
    {
        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; }
        
        [JsonPropertyName("event")]
        public string Event { get; set; }
        
        [JsonPropertyName("description")]
        public string Description { get; set; }
    }
}

