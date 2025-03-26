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

        [JsonPropertyName("entrance_room")]
        public EntranceRoom EntranceRoom { get; set; } = new EntranceRoom();

        [JsonPropertyName("puzzle_room")]
        public PuzzleRoom PuzzleRoom { get; set; } = new PuzzleRoom();

        [JsonPropertyName("setback_room")]
        public SetbackRoom SetbackRoom { get; set; } = new SetbackRoom();

        [JsonPropertyName("climax_room")]
        public ClimaxRoom ClimaxRoom { get; set; } = new ClimaxRoom();

        [JsonPropertyName("reward_room")]
        public RewardRoom RewardRoom { get; set; } = new RewardRoom();
    }

    public abstract class DelveRoomBase
    {
        protected DelveRoomBase()
        {
            Name = string.Empty;
            Description = string.Empty;
            Role = string.Empty;
        }

        [JsonPropertyName("room_number")]
        public int RoomNumber { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }
        
        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("valuables")]
        public List<DelveValuable> Valuables { get; set; } = new List<DelveValuable>();
    }

    public class EntranceRoom : DelveRoomBase
    {
        public EntranceRoom()
        {
            Role = "Entrance";
            RoomNumber = 1;
        }

        [JsonPropertyName("hazard_or_guardian")]
        public string HazardOrGuardian { get; set; } = string.Empty;
    }

    public class PuzzleRoom : DelveRoomBase
    {
        public PuzzleRoom()
        {
            Role = "Puzzle";
            RoomNumber = 2;
        }

        [JsonPropertyName("puzzle_or_roleplay_challenge")]
        public string PuzzleOrRoleplayChallenge { get; set; } = string.Empty;
    }

    public class SetbackRoom : DelveRoomBase
    {
        public SetbackRoom()
        {
            Role = "Setback";
            RoomNumber = 3;
        }

        [JsonPropertyName("trick_or_setback")]
        public string TrickOrSetback { get; set; } = string.Empty;
    }

    public class ClimaxRoom : DelveRoomBase
    {
        public ClimaxRoom()
        {
            Role = "Climax";
            RoomNumber = 4;
        }

        [JsonPropertyName("climax_conflict")]
        public string ClimaxConflict { get; set; } = string.Empty;

        [JsonPropertyName("hazard_or_guardian")]
        public string HazardOrGuardian { get; set; } = string.Empty;
    }

    public class RewardRoom : DelveRoomBase
    {
        public RewardRoom()
        {
            Role = "Reward";
            RoomNumber = 5;
        }

        [JsonPropertyName("reward_or_revelation")]
        public string RewardOrRevelation { get; set; } = string.Empty;
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
