using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AiGMBackEnd.Models.Locations
{
    public abstract class Location
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("knownToPlayer")]
        public bool KnownToPlayer { get; set; }

        [JsonPropertyName("connectedLocations")]
        public List<string> ConnectedLocations { get; set; } = new List<string>();

        [JsonPropertyName("parentLocation")]
        public string ParentLocation { get; set; }

        [JsonPropertyName("npcs")]
        public List<string> Npcs { get; set; } = new List<string>();
    }

}
