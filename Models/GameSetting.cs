using System.Text.Json.Serialization;

namespace AiGMBackEnd.Models
{
    public class GameSetting
    {
        [JsonPropertyName("tone")]
        public string Tone { get; set; } = "neutral";

        [JsonPropertyName("complexity")]
        public string Complexity { get; set; } = "medium";

        [JsonPropertyName("ageAppropriateness")]
        public string AgeAppropriateness { get; set; } = "teen";
    }
} 