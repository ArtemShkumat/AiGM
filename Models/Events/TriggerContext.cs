using System;

namespace AiGMBackEnd.Models
{
    public class TriggerContext
    {
        public string UserId { get; set; }
        
        public DateTimeOffset CurrentTime { get; set; }
        
        public string CurrentLocationId { get; set; }
        
        public string PreviousLocationId { get; set; }
        
        public string UserInput { get; set; }
        
        public World World { get; set; }
        
        public Player Player { get; set; }
    }
} 