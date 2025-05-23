using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AiGMBackEnd.Models
{
    // Base interface for update payloads
    public interface IUpdatePayload
    {
        [JsonPropertyName("type")]
        string Type { get; } // Readonly after deserialization
    }

    // --- Update Action Enum ---
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum UpdateAction { Add, Remove }

    // --- Nested Update Classes ---

    public class VisualDescriptionUpdatePayload
    {
        // Nullable properties for selective updates
        [JsonPropertyName("body")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Body { get; set; }

        [JsonPropertyName("condition")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Condition { get; set; }

        [JsonPropertyName("visibleClothing")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? VisibleClothing { get; set; }
    }

    public class InventoryUpdateItem
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("description")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Description { get; set; } // Optional for removal
        [JsonPropertyName("quantity")]
        [JsonConverter(typeof(LlmSafeIntConverter))]
        public int Quantity { get; set; }
        [JsonPropertyName("action")]
        public UpdateAction Action { get; set; }
    }

    public class CurrencyUpdateItem
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("amount")]
        [JsonConverter(typeof(LlmSafeIntConverter))]
        public int Amount { get; set; }
        [JsonPropertyName("action")]
        public UpdateAction Action { get; set; }
    }

     public class StatusEffectUpdateItem
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } // Name of the status effect
        [JsonPropertyName("action")]
        public UpdateAction Action { get; set; }
    }

    public class RpgTagUpdateItem
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("description")]
         [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Description { get; set; } // Needed for adding
        [JsonPropertyName("action")]
        public UpdateAction Action { get; set; }
    }

     public class ActiveQuestUpdateItem
    {
        [JsonPropertyName("questId")]
        public string QuestId { get; set; } // ID of the quest
        [JsonPropertyName("action")]
        public UpdateAction Action { get; set; }
    }


    public class KnownNpcUpdateItem
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } // Name or ID of the known NPC being updated/added/removed

        [JsonPropertyName("levelOfFamiliarity")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? LevelOfFamiliarity { get; set; } // Nullable for updates/removals

        [JsonPropertyName("disposition")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Disposition { get; set; } // Nullable for updates/removals

        [JsonPropertyName("action")]
        public UpdateAction Action { get; set; } // Add or Remove specific known NPC entry
    }

    public class KnownLocationUpdateItem
    {
         [JsonPropertyName("locationId")]
        public string LocationId { get; set; } // ID of the location
        [JsonPropertyName("action")]
        public UpdateAction Action { get; set; } // Add or Remove
    }

     public class NpcListUpdateItem // For Location.Npcs
    {
         [JsonPropertyName("npcId")]
        public string NpcId { get; set; } // ID of the NPC
        [JsonPropertyName("action")]
        public UpdateAction Action { get; set; } // Add or Remove
    }

    // --- Concrete Update Payload Classes ---

    public class PlayerUpdatePayload : IUpdatePayload
    {
        [JsonPropertyName("type")]
        public string Type => "PLAYER";

        [JsonPropertyName("currentLocationId")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? CurrentLocationId { get; set; }

        [JsonPropertyName("visualDescription")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public VisualDescriptionUpdatePayload? VisualDescription { get; set; }

        [JsonPropertyName("inventory")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<InventoryUpdateItem>? Inventory { get; set; }

        [JsonPropertyName("currencies")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<CurrencyUpdateItem>? Currencies { get; set; }

        [JsonPropertyName("statusEffects")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<StatusEffectUpdateItem>? StatusEffects { get; set; }

        [JsonPropertyName("rpgTags")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<RpgTagUpdateItem>? RpgTags { get; set; }

        [JsonPropertyName("activeQuests")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<ActiveQuestUpdateItem>? ActiveQuests { get; set; }
    }

    public class WorldUpdatePayload : IUpdatePayload
    {
        [JsonPropertyName("type")]
        public string Type => "WORLD";

        [JsonPropertyName("timeDelta")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public TimeDelta? TimeDelta { get; set; }
    }

    public class NpcUpdatePayload : IUpdatePayload
    {
        [JsonPropertyName("type")]
        public string Type => "NPC";

        [JsonPropertyName("id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Id { get; set; }

        [JsonPropertyName("currentLocationId")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? CurrentLocationId { get; set; }

        [JsonPropertyName("visualDescription")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public VisualDescriptionUpdatePayload? VisualDescription { get; set; }

        [JsonPropertyName("currentGoal")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? CurrentGoal { get; set; }

        [JsonPropertyName("dispositionTowardsPlayer")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? DispositionTowardsPlayer { get; set; }

        [JsonPropertyName("inventory")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<InventoryUpdateItem>? Inventory { get; set; }
    }

     public class LocationUpdatePayload : IUpdatePayload
    {
        [JsonPropertyName("type")]
        public string Type => "LOCATION"; // Base type for identification

        [JsonPropertyName("id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Id { get; set; }

         [JsonPropertyName("parentLocation")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ParentLocation { get; set; }
    }
} 