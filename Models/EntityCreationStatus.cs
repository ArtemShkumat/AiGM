using System;

namespace AiGMBackEnd.Models
{
    public class EntityCreationStatus
    {
        public string EntityId { get; set; }
        public string EntityType { get; set; }
        public string Status { get; set; } // "pending", "complete", "error"
        public string ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
} 