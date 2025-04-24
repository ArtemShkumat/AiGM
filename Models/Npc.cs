using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AiGMBackEnd.Models
{
    public class Npc
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "NPC";

        [JsonPropertyName("id")]
        public string Id { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("currentLocationId")]
        public string CurrentLocationId { get; set; }
        
        [JsonPropertyName("visualDescription")]
        public VisualDescription VisualDescription { get; set; } = new VisualDescription();
        
        [JsonPropertyName("personality")]
        public Personality Personality { get; set; } = new Personality();
        
        [JsonPropertyName("backstory")]
        public string Backstory { get; set; }
        [JsonPropertyName("race")]
        public string Race { get; set; }

        [JsonPropertyName("currentGoal")]
        public string CurrentGoal { get; set; }

        [JsonPropertyName("age")]
        public int Age { get; set; }

        [JsonPropertyName("dispositionTowardsPlayer")]
        public string DispositionTowardsPlayer { get; set; }     
        
        [JsonPropertyName("inventory")]
        public List<InventoryItem> Inventory { get; set; } = new List<InventoryItem>();
        
        [JsonPropertyName("conversationLog")]
        public List<Dictionary<string, string>> ConversationLog { get; set; } = new List<Dictionary<string, string>>();

        [JsonPropertyName("isDeceased")]
        public bool IsDeceased { get; set; } = false;

        [JsonPropertyName("statBlockId")]
        public string? StatBlockId { get; set; } // Nullable string
    }

    public class Personality
    {
        [JsonPropertyName("temperament")]
        public string Temperament { get; set; }
        
        [JsonPropertyName("traits")]
        public string Traits { get; set; }

        [JsonPropertyName("motivations")]
        public string Motivations { get; set; }

        [JsonPropertyName("fears")]
        public string Fears { get; set; }

        [JsonPropertyName("secrets")]
        public List<string> Secrets { get; set; } = new List<string>();

    }    
}
