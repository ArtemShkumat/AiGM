using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AiGMBackEnd.Models
{
    public class SceneContext
    {
        [JsonPropertyName("gameTime")]
        public string GameTime { get; set; }
        
        [JsonPropertyName("playerInfo")]
        public ScenePlayerInfo PlayerInfo { get; set; } = new ScenePlayerInfo();
        
        [JsonPropertyName("locationInfo")]
        public SceneLocationInfo LocationInfo { get; set; } = new SceneLocationInfo();
        
        [JsonPropertyName("relevantQuests")]
        public List<RelevantQuest> RelevantQuests { get; set; } = new List<RelevantQuest>();
        
        [JsonPropertyName("recentConversations")]
        public List<RecentConversation> RecentConversations { get; set; } = new List<RecentConversation>();
    }

    public class ScenePlayerInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("visualDescription")]
        public VisualDescription VisualDescription { get; set; } = new VisualDescription();
        
        [JsonPropertyName("reputationWithNPC")]
        public string ReputationWithNPC { get; set; }
        
        [JsonPropertyName("knownToNPC")]
        public bool KnownToNPC { get; set; }
    }

    public class SceneLocationInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("description")]
        public string Description { get; set; }
        
        [JsonPropertyName("pointsOfInterest")]
        public List<PointOfInterest> PointsOfInterest { get; set; } = new List<PointOfInterest>();
    }

    public class RelevantQuest
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        
        [JsonPropertyName("title")]
        public string Title { get; set; }
        
        [JsonPropertyName("progress")]
        public string Progress { get; set; }
        
        [JsonPropertyName("npcInvolvement")]
        public string NpcInvolvement { get; set; }
    }

    public class RecentConversation
    {
        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; }
        
        [JsonPropertyName("summary")]
        public string Summary { get; set; }
    }
}

