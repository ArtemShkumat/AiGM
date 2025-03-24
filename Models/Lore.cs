using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AiGMBackEnd.Models
{
    public class Lore
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "LORE";
        
        [JsonPropertyName("id")]
        public string Id { get; set; }
        
        [JsonPropertyName("title")]
        public string Title { get; set; }
        
        [JsonPropertyName("summary")]
        public string Summary { get; set; }        
    }

}

