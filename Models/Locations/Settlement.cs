using System.Collections.Generic;
using System.Text.Json.Serialization;
using AiGMBackEnd.Models.Locations;

namespace AiGMBackEnd.Models
{
    public class Settlement : Location
    {
        public Settlement()
        {
            Type = "Settlement";
            Purpose = string.Empty;
            History = string.Empty;
            Size = string.Empty;
        }

        [JsonPropertyName("purpose")]
        public string Purpose { get; set; }

        [JsonPropertyName("history")]
        public string History { get; set; }

        [JsonPropertyName("size")]
        public string Size { get; set; }

        [JsonPropertyName("population")]
        public int Population { get; set; }

        [JsonPropertyName("district_ids")]
        public List<string> DistrictIds { get; set; } = new List<string>();
    }
} 