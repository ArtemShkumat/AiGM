using System.Collections.Generic;

namespace AiGMBackEnd.Models
{
    public class Quest
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string GiverNpcId { get; set; }
        public string Status { get; set; } // Available, Active, Completed, Failed
        public List<QuestStep> Steps { get; set; } = new List<QuestStep>();
        public List<QuestReward> Rewards { get; set; } = new List<QuestReward>();
        public List<string> Prerequisites { get; set; } = new List<string>();
        public Dictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();
    }

    public class QuestStep
    {
        public string Id { get; set; }
        public string Description { get; set; }
        public string LocationId { get; set; }
        public string TargetNpcId { get; set; }
        public string TargetItemId { get; set; }
        public bool IsCompleted { get; set; }
        public List<string> NextStepIds { get; set; } = new List<string>();
        public Dictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();
    }

    public class QuestReward
    {
        public string Type { get; set; } // Item, Experience, Gold, Reputation
        public string TargetId { get; set; } // Item ID or faction ID for reputation
        public int Amount { get; set; }
        public string Description { get; set; }
    }
}
