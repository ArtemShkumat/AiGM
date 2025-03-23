using System.Text.Json.Serialization;

namespace AiGMBackEnd.Models
{
    public class InventoryItem
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }
    }
}
