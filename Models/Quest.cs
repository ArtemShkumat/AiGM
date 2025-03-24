using System.Collections.Generic;
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
        
        [JsonPropertyName("currentProgress")]
        public string CurrentProgress { get; set; }
        
        [JsonPropertyName("questDescription")]
        public string QuestDescription { get; set; }
        
        [JsonPropertyName("achievementConditions")]
        public List<string> AchievementConditions { get; set; } = new List<string>();
        
        [JsonPropertyName("failConditions")]
        public List<string> FailConditions { get; set; } = new List<string>();
        
        [JsonPropertyName("involvedLocations")]
        public List<string> InvolvedLocations { get; set; } = new List<string>();
        
        [JsonPropertyName("involvedNpcs")]
        public List<string> InvolvedNpcs { get; set; } = new List<string>();
    }    
}
