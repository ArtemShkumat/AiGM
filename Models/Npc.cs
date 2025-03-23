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

        [JsonPropertyName("knownToPlayer")]
        public bool KnownToPlayer { get; set; }

        [JsonPropertyName("knowsPlayer")]
        public bool KnowsPlayer { get; set; }

        [JsonPropertyName("visibleToPlayer")]
        public bool VisibleToPlayer { get; set; }
        
        [JsonPropertyName("visualDescription")]
        public VisualDescription VisualDescription { get; set; } = new VisualDescription();
        
        [JsonPropertyName("personality")]
        public Personality Personality { get; set; } = new Personality();
        
        [JsonPropertyName("backstory")]
        public string Backstory { get; set; }

        [JsonPropertyName("currentGoal")]
        public string CurrentGoal { get; set; }

        [JsonPropertyName("age")]
        public int Age { get; set; }

        [JsonPropertyName("dispositionTowardsPlayer")]
        public string DispositionTowardsPlayer { get; set; }
        
        [JsonPropertyName("knownEntities")]
        public KnownEntities KnownEntities { get; set; } = new KnownEntities();        
        
        [JsonPropertyName("questInvolvement")]
        public List<string> QuestInvolvement { get; set; } = new List<string>();
        
        [JsonPropertyName("inventory")]
        public List<InventoryItem> Inventory { get; set; } = new List<InventoryItem>();
        
        [JsonPropertyName("conversationLog")]
        public List<Dictionary<string, string>> ConversationLog { get; set; } = new List<Dictionary<string, string>>();
    }

    public class Personality
    {
        [JsonPropertyName("temperament")]
        public string Temperament { get; set; }
        
        [JsonPropertyName("traits")]
        public string Quirks { get; set; }

        [JsonPropertyName("motivations")]
        public string Motivations { get; set; }

        [JsonPropertyName("fears")]
        public string Fears { get; set; }

        [JsonPropertyName("secrets")]
        public List<string> Secrets { get; set; } = new List<string>();

    }

    public class KnownEntities
    {
        [JsonPropertyName("npcsKnown")]
        public List<NpcsKnownDetails> NpcsKnown { get; set; } = new List<NpcsKnownDetails>();
        
        [JsonPropertyName("locationsKnown")]
        public List<string> LocationsKnown { get; set; } = new List<string>();
    }

    public class NpcsKnownDetails
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("levelOfFamiliarity")]
        public string LevelOfFamiliarity { get; set; }// Aware/Met/Known/Familiar/Deep

        [JsonPropertyName("disposition")]
        public string Disposition { get; set; }//Hostile/Unfriendly/Neutral/Fond/Loyal

    }
}
