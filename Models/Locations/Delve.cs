using System.Collections.Generic;
using System.Text.Json.Serialization;
using AiGMBackEnd.Models.Locations;

namespace AiGMBackEnd.Models
{
    public class Delve : Location
    {
        public Delve()
        {
            Type = "Delve";
            Purpose = string.Empty;
        }

        [JsonPropertyName("purpose")]
        public string Purpose { get; set; }

        [JsonPropertyName("delve_room_ids")]
        public List<string> DelveRoomIds { get; set; } = new List<string>();
    }
}
