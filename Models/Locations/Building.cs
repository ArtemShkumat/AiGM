using System.Collections.Generic;
using System.Text.Json.Serialization;
using AiGMBackEnd.Models.Locations;

namespace AiGMBackEnd.Models
{
    public class Building : Location
    {
        public Building()
        {
            Purpose = string.Empty;
            History = string.Empty;
            ExteriorDescription = string.Empty;
        }

        [JsonPropertyName("exterior_description")]
        public string ExteriorDescription { get; set; }

        [JsonPropertyName("purpose")]
        public string Purpose { get; set; }

        [JsonPropertyName("history")]
        public string History { get; set; }

        [JsonPropertyName("floor_ids")]
        public List<string> FloorIds { get; set; } = new List<string>();
    }
} 