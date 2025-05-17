using System;
using System.Text.Json.Serialization;

namespace AiGMBackEnd.Models
{
    [JsonConverter(typeof(TriggerValueConverter))]
    public abstract class TriggerValue
    {
        [JsonPropertyName("type")]
        public abstract string Type { get; }
        
        public abstract bool Equals(TriggerValue other);
    }
} 