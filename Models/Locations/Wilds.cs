using System.Collections.Generic;
using System.Text.Json.Serialization;
using AiGMBackEnd.Models.Locations;

namespace AiGMBackEnd.Models
{
    public class Wilds : Location
    {
        public Wilds()
        {
            Type = "Wilds";
            Terrain = string.Empty;
            Dangers = string.Empty;
            LocationType = "Wilds";
        }

        [JsonPropertyName("terrain")]
        public string Terrain { get; set; }

        [JsonPropertyName("dangers")]
        public string Dangers { get; set; }

        [JsonPropertyName("danger_level")]
        public int DangerLevel { get; set; }

        [JsonPropertyName("points_of_interest")]
        public List<PointOfInterest> PointsOfInterest { get; set; } = new List<PointOfInterest>();
    }
} 