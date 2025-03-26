using System.Collections.Generic;
using System.Text.Json.Serialization;
using AiGMBackEnd.Models.Locations;

namespace AiGMBackEnd.Models
{
    public class Delve : Location
    {
        public Delve()
        {
            Type = "Delve";
            Purpose = string.Empty;
        }

        [JsonPropertyName("purpose")]
        public string Purpose { get; set; }

        [JsonPropertyName("rooms")]
        public List<DelveRoom> Rooms { get; set; } = new List<DelveRoom>();
    }

    public class DelveRoom
    {
        public DelveRoom()
        {
            Role = string.Empty;
            Name = string.Empty;
            Description = string.Empty;
            HazardOrGuardian = string.Empty;
            PuzzleOrRoleplayChallenge = string.Empty;
            TrickOrSetback = string.Empty;
            ClimaxConflict = string.Empty;
            RewardOrRevelation = string.Empty;
        }
        
        [JsonPropertyName("room_number")]
        public int RoomNumber { get; set; }

        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("hazard_or_guardian")]
        public string HazardOrGuardian { get; set; }

        [JsonPropertyName("puzzle_or_roleplay_challenge")]
        public string PuzzleOrRoleplayChallenge { get; set; }

        [JsonPropertyName("trick_or_setback")]
        public string TrickOrSetback { get; set; }

        [JsonPropertyName("climax_conflict")]
        public string ClimaxConflict { get; set; }

        [JsonPropertyName("reward_or_revelation")]
        public string RewardOrRevelation { get; set; }

        [JsonPropertyName("valuables")]
        public List<DelveValuable> Valuables { get; set; } = new List<DelveValuable>();
    }

    public class DelveValuable
    {
        public DelveValuable()
        {
            Name = string.Empty;
            WhyItsHere = string.Empty;
            Description = string.Empty;
            WhereExactly = string.Empty;
        }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("why_its_here")]
        public string WhyItsHere { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("value")]
        public int Value { get; set; }

        [JsonPropertyName("where_exactly")]
        public string WhereExactly { get; set; }
    }
}
