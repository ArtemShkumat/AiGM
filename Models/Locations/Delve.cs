namespace AiGMBackEnd.Models.Locations
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class Delve
    {
        [JsonPropertyName("core_identity")]
        public CoreIdentity CoreIdentity { get; set; }

        [JsonPropertyName("navigation")]
        public Navigation Navigation { get; set; }

        [JsonPropertyName("rooms")]
        public List<DelveRoom> Rooms { get; set; }
    }

    public class CoreIdentity
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("purpose")]
        public string Purpose { get; set; }
    }

    public class Navigation
    {
        [JsonPropertyName("parent_location")]
        public string ParentLocation { get; set; }

        [JsonPropertyName("connected_locations")]
        public List<string> ConnectedLocations { get; set; }
    }

    public class DelveRoom
    {
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
        public List<Valuable> Valuables { get; set; }
    }

    public class Valuable
    {
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
