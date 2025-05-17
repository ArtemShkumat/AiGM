using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AiGMBackEnd.Models
{
    public class Event
    {
        public string Id { get; set; }
        
        public string Summary { get; set; }
        
        public EventType TriggerType { get; set; }
        
        [JsonConverter(typeof(TriggerValueConverter))]
        public TriggerValue TriggerValue { get; set; }
        
        public Dictionary<string, object> Context { get; set; } = new Dictionary<string, object>();
        
        public EventStatus Status { get; set; }
        
        public DateTimeOffset CreationTime { get; set; }
        
        public DateTimeOffset? CompletionTime { get; set; }
    }
} 