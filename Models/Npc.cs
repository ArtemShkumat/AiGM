using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AiGMBackEnd.Models
{
    public class Npc
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("currentLocationId")]
        public string CurrentLocationId { get; set; }
        
        [JsonPropertyName("discoveredByPlayer")]
        public bool DiscoveredByPlayer { get; set; }
        
        [JsonPropertyName("visibleToPlayer")]
        public bool VisibleToPlayer { get; set; }
        
        [JsonPropertyName("visualDescription")]
        public VisualDescription VisualDescription { get; set; } = new VisualDescription();
        
        [JsonPropertyName("personality")]
        public Personality Personality { get; set; } = new Personality();
        
        [JsonPropertyName("backstory")]
        public string Backstory { get; set; }

        [JsonPropertyName("age")]
        public int Age { get; set; }

        [JsonPropertyName("dispositionTowardsPlayer")]
        public string DispositionTowardsPlayer { get; set; }
        
        [JsonPropertyName("knownEntities")]
        public KnownEntities KnownEntities { get; set; } = new KnownEntities();
        
        [JsonPropertyName("relationships")]
        public List<Relationship> Relationships { get; set; } = new List<Relationship>();
        
        [JsonPropertyName("questInvolvement")]
        public List<string> QuestInvolvement { get; set; } = new List<string>();
        
        [JsonPropertyName("inventory")]
        public List<InventoryItem> Inventory { get; set; } = new List<InventoryItem>();
        
        [JsonPropertyName("statusFlags")]
        public StatusFlags StatusFlags { get; set; } = new StatusFlags();
        
        [JsonPropertyName("notes")]
        public string Notes { get; set; }
        
        [JsonPropertyName("conversationLog")]
        public List<Dictionary<string, string>> ConversationLog { get; set; } = new List<Dictionary<string, string>>();
    }

    public class Personality
    {
        [JsonPropertyName("temperament")]
        public string Temperament { get; set; }
        
        [JsonPropertyName("traits")]
        public string Traits { get; set; }
    }

    public class KnownEntities
    {
        [JsonPropertyName("npcsKnown")]
        public List<string> NpcsKnown { get; set; } = new List<string>();
        
        [JsonPropertyName("locationsKnown")]
        public List<string> LocationsKnown { get; set; } = new List<string>();
    }

    public class StatusFlags
    {
        [JsonPropertyName("isAlive")]
        public bool IsAlive { get; set; } = true;
        
        [JsonPropertyName("isBusy")]
        public bool IsBusy { get; set; }
        
        [JsonPropertyName("customState")]
        public string CustomState { get; set; }
    }
}
