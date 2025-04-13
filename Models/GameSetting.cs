using System.Text.Json.Serialization;

namespace AiGMBackEnd.Models
{
    public class GameSetting
    {

        [JsonPropertyName("genre")]
        public string Genre { get; set; } = "fantasy";

        [JsonPropertyName("theme")]
        public string Theme { get; set; } = "adventure";

        [JsonPropertyName("description")]
        public string Description { get; set; } = "A standard fantasy adventure in a magical world.";

        [JsonPropertyName("startingLocation")]
        public string StartingLocation { get; set; }

        [JsonPropertyName("gameName")]
        public string GameName { get; set; }

        [JsonPropertyName("setting")]
        public string Setting { get; set; }

        [JsonPropertyName("currencies")]
        public List<string> Currencies { get; set; } = new List<string>();
        [JsonPropertyName("economy")]
        public string Economy { get; set; }
    }
} 