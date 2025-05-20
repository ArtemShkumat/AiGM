using System.Collections.Generic;
using System.Text.Json.Serialization;
using AiGMBackEnd.Models;

namespace AIGM.Models
{
    public class EventCreationHook : ICreationHook
    {
        [JsonPropertyName("type")]
        public string Type => "event";
        
        [JsonPropertyName("id")]
        public string Id { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("context")]
        public string Context { get; set; }
        
        [JsonPropertyName("summary")]
        public string Summary { get; set; }
        
        [JsonPropertyName("triggerType")]
        public EventType TriggerType { get; set; }
        
        [JsonPropertyName("triggerValue")]
        [JsonConverter(typeof(TriggerValueConverter))]
        public TriggerValue TriggerValue { get; set; }
        
        [JsonPropertyName("additionalContext")]
        public Dictionary<string, object> AdditionalContext { get; set; } = new Dictionary<string, object>();
    }
} 