using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AiGMBackEnd.Models.Locations
{
    [JsonConverter(typeof(LocationConverter))]
    public abstract class Location
    {
        // Add parameterless constructor for deserialization
        public Location() { }

        [JsonPropertyName("type")] 
        public string Type { get; set; } = "LOCATION";

        [JsonPropertyName("locationType")]
        public string LocationType { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("parentLocation")]
        public string ParentLocation { get; set; }

        [JsonPropertyName("typicalOccupants")]
        public string TypicalOccupants { get; set; } = string.Empty;
    }
}
