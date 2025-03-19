using System.Collections.Generic;

namespace AiGMBackEnd.Models
{
    public class Player
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string CurrentLocationId { get; set; }
        public Dictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();
        public List<string> Inventory { get; set; } = new List<string>();
        public List<string> ActiveQuestIds { get; set; } = new List<string>();
        public List<string> CompletedQuestIds { get; set; } = new List<string>();
        public Dictionary<string, int> Relationships { get; set; } = new Dictionary<string, int>();
    }
}
