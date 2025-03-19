using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AiGMBackEnd.Models
{
    public class Player
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("currentLocationId")]
        public string CurrentLocationId { get; set; }
        
        [JsonPropertyName("visualDescription")]
        public VisualDescription VisualDescription { get; set; } = new VisualDescription();
        
        [JsonPropertyName("backstory")]
        public string Backstory { get; set; }
        
        [JsonPropertyName("relationships")]
        public List<Relationship> Relationships { get; set; } = new List<Relationship>();
        
        [JsonPropertyName("inventory")]
        public List<InventoryItem> Inventory { get; set; } = new List<InventoryItem>();
        
        [JsonPropertyName("money")]
        public int Money { get; set; }
        
        [JsonPropertyName("statusEffects")]
        public List<string> StatusEffects { get; set; } = new List<string>();
        
        [JsonPropertyName("rpgElements")]
        public Dictionary<string, object> RpgElements { get; set; } = new Dictionary<string, object>();
        
        [JsonPropertyName("activeQuests")]
        public List<string> ActiveQuests { get; set; } = new List<string>();
        
        [JsonPropertyName("playerLog")]
        public List<LogEntry> PlayerLog { get; set; } = new List<LogEntry>();
        
        [JsonPropertyName("notes")]
        public string Notes { get; set; }
    }

    public class VisualDescription
    {
        [JsonPropertyName("gender")]
        public string Gender { get; set; }
        
        [JsonPropertyName("bodyType")]
        public string BodyType { get; set; }
        
        [JsonPropertyName("visibleClothing")]
        public string VisibleClothing { get; set; }
        
        [JsonPropertyName("condition")]
        public string Condition { get; set; }
    }

    public class Relationship
    {
        [JsonPropertyName("npcId")]
        public string NpcId { get; set; }
        
        [JsonPropertyName("relationship")]
        public string RelationshipType { get; set; }
    }

    public class InventoryItem
    {
        [JsonPropertyName("itemId")]
        public string ItemId { get; set; }
        
        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }
    }

    public class LogEntry
    {
        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; }
        
        [JsonPropertyName("event")]
        public string Event { get; set; }
        
        [JsonPropertyName("description")]
        public string Description { get; set; }
    }
}
