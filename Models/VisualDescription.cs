using System.Text.Json.Serialization;

namespace AiGMBackEnd.Models
{
    public class VisualDescription
    {

        [JsonPropertyName("bodyType")]
        public string Body { get; set; }

        [JsonPropertyName("condition")]
        public string Condition { get; set; }
        [JsonPropertyName("gender")]
        public string Gender { get; set; }

        [JsonPropertyName("resemblingCelebrity")]
        public string ResemblingCelebrity { get; set; }

        [JsonPropertyName("visibleClothing")]
        public string VisibleClothing { get; set; }
    }
}