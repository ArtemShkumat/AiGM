using System.Text.Json.Serialization;

namespace AiGMBackEnd.Models
{
    public class Valuable
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("why_its_here")]
        public string WhyItsHere { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("value")]
        public int Value { get; set; }
    }
} 