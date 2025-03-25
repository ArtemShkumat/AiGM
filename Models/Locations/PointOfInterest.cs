using System.Text.Json.Serialization;

namespace AiGMBackEnd.Models
{
    public class PointOfInterest
    {
        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("hinting_at")]
        public string HintingAt { get; set; }
    }
} 