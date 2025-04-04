using System.Text.Json.Serialization;

namespace AiGMBackEnd.Models
{
    public class RpgTag
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("description")]
        public string Description { get; set; }
    }
} 