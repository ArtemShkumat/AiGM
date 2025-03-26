using System.Text.Json.Serialization;

namespace AiGMBackEnd.Models
{
    public class PointOfInterest
    {
        public PointOfInterest()
        {
            Name = string.Empty;
            Description = string.Empty;
            HintingAt = string.Empty;
        }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("hinting_at")]
        public string HintingAt { get; set; }
    }
} 