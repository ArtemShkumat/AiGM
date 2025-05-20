using System;
using System.Text.Json.Serialization;

namespace AiGMBackEnd.Models
{
    public class TimeTriggerValue : TriggerValue
    {
        private const string TYPE_NAME = "time";
        
        [JsonPropertyName("triggerTime")]
        public DateTimeOffset TriggerTime { get; set; }
        
        [JsonIgnore]
        public override string Type => TYPE_NAME;
        
        public override bool Equals(TriggerValue other)
        {
            if (other is not TimeTriggerValue timeTrigger)
                return false;
                
            return TriggerTime.Equals(timeTrigger.TriggerTime);
        }
    }
} 