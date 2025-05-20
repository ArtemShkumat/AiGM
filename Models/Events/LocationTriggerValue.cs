using System;
using System.Text.Json.Serialization;

namespace AiGMBackEnd.Models
{
    public class LocationTriggerValue : TriggerValue
    {
        private const string TYPE_NAME = "location";
        
        [JsonPropertyName("locationId")]
        public string LocationId { get; set; }
        
        [JsonPropertyName("mustBeFirstVisit")]
        public bool MustBeFirstVisit { get; set; }
        
        [JsonIgnore]
        public override string Type => TYPE_NAME;
        
        public override bool Equals(TriggerValue other)
        {
            if (other is not LocationTriggerValue locationTrigger)
                return false;
                
            return LocationId == locationTrigger.LocationId && 
                   MustBeFirstVisit == locationTrigger.MustBeFirstVisit;
        }
    }
} 