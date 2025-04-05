using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AiGMBackEnd.Models
{
    public class RecentEvents
    {
        [JsonPropertyName("messages")]
        public List<Dictionary<string, string>> Messages { get; set; } = new List<Dictionary<string, string>>();
    }
} 