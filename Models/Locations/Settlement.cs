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

        [JsonPropertyName("districts")]
        public List<District> Districts { get; set; } = new List<District>();
    }

    public class District
    {
        public District()
        {
            Name = string.Empty;
            Description = string.Empty;
            ConnectedDistricts = new List<string>();
            PointsOfInterest = new List<PointOfInterest>();
            Npcs = new List<string>();
            Buildings = new List<string>();
        }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("connected_districts")]
        public List<string> ConnectedDistricts { get; set; } = new List<string>();

        [JsonPropertyName("points_of_interest")]
        public List<PointOfInterest> PointsOfInterest { get; set; } = new List<PointOfInterest>();

        [JsonPropertyName("npcs")]
        public List<string> Npcs { get; set; } = new List<string>();

        [JsonPropertyName("buildings")]
        public List<string> Buildings { get; set; } = new List<string>();
    }
} 