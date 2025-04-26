using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AiGMBackEnd.Models
{
    public class World
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "WORLD";       
        
        [JsonPropertyName("gameTime")]
        public DateTimeOffset GameTime { get; set; }
        
        [JsonPropertyName("currentPlayer")]
        public string CurrentPlayer { get; set; }
        
        [JsonPropertyName("locations")]
        public List<LocationSummary> Locations { get; set; } = new List<LocationSummary>();
        
        [JsonPropertyName("npcs")]
        public List<NpcSummary> Npcs { get; set; } = new List<NpcSummary>();
        
        [JsonPropertyName("quests")]
        public List<QuestSummary> Quests { get; set; } = new List<QuestSummary>();
        
        /// <summary>
        /// Advances the game time based on a time delta
        /// </summary>
        /// <param name="delta">The time delta to apply</param>
        public void AdvanceTime(TimeDelta delta)
        {
            if (delta == null)
                return;
                
            switch (delta.Unit.ToLower())
            {
                case "seconds":
                    GameTime = GameTime.AddSeconds(delta.Amount);
                    break;
                case "minutes":
                    GameTime = GameTime.AddMinutes(delta.Amount);
                    break;
                case "hours":
                    GameTime = GameTime.AddHours(delta.Amount);
                    break;
                case "days":
                    GameTime = GameTime.AddDays(delta.Amount);
                    break;
                default:
                    throw new ArgumentException($"Unknown time unit: {delta.Unit}");
            }
        }
    }
   
    public class LocationSummary
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("locationType")]
        public string LocationType { get; set; }
    }

    public class NpcSummary
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public class QuestSummary
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        
        [JsonPropertyName("title")]
        public string Title { get; set; }
    }   
}
