using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AiGMBackEnd.Models
{
    public class Player
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "Player";

        [JsonPropertyName("id")]
        public string Id { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("currentLocationId")]
        public string CurrentLocationId { get; set; }
        
        [JsonPropertyName("visualDescription")]
        public VisualDescription VisualDescription { get; set; } = new VisualDescription();

        [JsonPropertyName("age")]
        public int Age { get; set; }

        [JsonPropertyName("backstory")]
        public string Backstory { get; set; }

        [JsonPropertyName("inventory")]
        public List<InventoryItem> Inventory { get; set; } = new List<InventoryItem>();
        
        [JsonPropertyName("money")]
        public int Money { get; set; }
        
        [JsonPropertyName("statusEffects")]
        public List<string> StatusEffects { get; set; } = new List<string>();
        
        [JsonPropertyName("rpgTags")]
        public List<RpgTag> RpgTags { get; set; } = new List<RpgTag>();
        
        [JsonPropertyName("activeQuests")]
        public List<string> ActiveQuests { get; set; } = new List<string>();        
    }
}
