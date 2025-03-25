using System.Text.Json.Serialization;

namespace AiGMBackEnd.Models
{
    public class RoomNpc
    {
        [JsonPropertyName("npcId")]
        public string NpcId { get; set; }
    }
} 