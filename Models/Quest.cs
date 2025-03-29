using System.Text.Json.Serialization;

namespace AiGMBackEnd.Models
{
    public class Quest
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "QUEST";
        
        [JsonPropertyName("id")]
        public string Id { get; set; }
        
        [JsonPropertyName("title")]
        public string Title { get; set; }
        
        [JsonPropertyName("coreObjective")]
        public string CoreObjective { get; set; }
        
        [JsonPropertyName("overview")]
        public string Overview { get; set; }
        
        [JsonPropertyName("npcs")]
        public List<QuestNpc> Npcs { get; set; } = new List<QuestNpc>();
        
        [JsonPropertyName("rumorsAndLeads")]
        public List<RumorAndLead> RumorsAndLeads { get; set; } = new List<RumorAndLead>();
        
        [JsonPropertyName("locationsInvolved")]
        public List<QuestLocation> LocationsInvolved { get; set; } = new List<QuestLocation>();
        
        [JsonPropertyName("opposingForces")]
        public List<OpposingForce> OpposingForces { get; set; } = new List<OpposingForce>();
        
        [JsonPropertyName("challenges")]
        public List<string> Challenges { get; set; } = new List<string>();
        
        [JsonPropertyName("emotionalBeats")]
        public List<string> EmotionalBeats { get; set; } = new List<string>();
        
        [JsonPropertyName("rewards")]
        public List<string> Rewards { get; set; } = new List<string>();
    }

    public class QuestNpc
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("role")]
        public string Role { get; set; }
        
        [JsonPropertyName("motivation")]
        public string Motivation { get; set; }
        
        [JsonPropertyName("fears")]
        public string Fears { get; set; }
        
        [JsonPropertyName("secrets")]
        public string Secrets { get; set; }
    }

    public class RumorAndLead
    {
        [JsonPropertyName("rumor")]
        public string Rumor { get; set; }
        
        [JsonPropertyName("sourceNPC")]
        public string SourceNPC { get; set; }
        
        [JsonPropertyName("sourceLocation")]
        public string SourceLocation { get; set; }
    }

    public class QuestLocation
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }        
        
        [JsonPropertyName("type")]
        public string Type { get; set; }
    }

    public class OpposingForce
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("role")]
        public string Role { get; set; }
        
        [JsonPropertyName("motivation")]
        public string Motivation { get; set; }
        
        [JsonPropertyName("description")]
        public string Description { get; set; }
    }
}
