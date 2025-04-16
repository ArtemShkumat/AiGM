using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AiGMBackEnd.Models
{
    public class PartialUpdates
    {
        [JsonPropertyName("player")]
        public PlayerUpdatePayload Player { get; set; }

        [JsonPropertyName("world")]
        public WorldUpdatePayload World { get; set; }

        [JsonPropertyName("npcEntries")]
        public List<NpcUpdatePayload> NpcEntries { get; set; }

        [JsonPropertyName("locationEntries")]
        public List<LocationUpdatePayload> LocationEntries { get; set; }

        public PartialUpdates()
        {
            NpcEntries = new List<NpcUpdatePayload>();
            LocationEntries = new List<LocationUpdatePayload>();
        }
    }
} 