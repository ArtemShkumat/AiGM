using System.Text.Json.Serialization;

namespace AiGMBackEnd.Models
{
    public class TimeDelta
    {
        [JsonPropertyName("amount")]
        [JsonConverter(typeof(LlmSafeIntConverter))]
        public int Amount { get; set; }

        [JsonPropertyName("unit")]
        public string Unit { get; set; }
    }
} 