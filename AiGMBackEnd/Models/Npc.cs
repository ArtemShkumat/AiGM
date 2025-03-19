using System.Collections.Generic;

namespace AiGMBackEnd.Models
{
    public class Npc
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string LocationId { get; set; }
        public Dictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();
        public string Personality { get; set; }
        public List<string> KnownInformation { get; set; } = new List<string>();
        public Dictionary<string, int> Relationships { get; set; } = new Dictionary<string, int>();
        public List<string> AvailableQuestIds { get; set; } = new List<string>();
        public List<string> Inventory { get; set; } = new List<string>();
        public string Schedule { get; set; }
    }
}
