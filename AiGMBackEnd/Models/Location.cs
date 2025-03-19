using System.Collections.Generic;

namespace AiGMBackEnd.Models
{
    public class Location
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Dictionary<string, string> Connections { get; set; } = new Dictionary<string, string>();
        public List<string> NpcIds { get; set; } = new List<string>();
        public List<string> ItemIds { get; set; } = new List<string>();
        public Dictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();
        public string Atmosphere { get; set; }
        public List<string> PointsOfInterest { get; set; } = new List<string>();
    }
}
