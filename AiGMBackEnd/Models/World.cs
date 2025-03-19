using System.Collections.Generic;

namespace AiGMBackEnd.Models
{
    public class World
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Setting { get; set; } // Fantasy, SciFi, Modern, etc.
        public string Tone { get; set; } // Light, Dark, Comedic, Serious, etc.
        public int CurrentDay { get; set; }
        public string CurrentWeather { get; set; }
        public string CurrentTime { get; set; }
        public List<string> GlobalEvents { get; set; } = new List<string>();
        public Dictionary<string, object> Flags { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, string> Factions { get; set; } = new Dictionary<string, string>();
    }
}
