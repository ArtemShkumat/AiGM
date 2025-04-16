using System.Collections.Generic;
using System.Text.Json.Serialization;
using AiGMBackEnd.Models; // Needed for converters and interfaces

namespace AiGMBackEnd.Models
{
    /// <summary>
    /// Represents the structured response expected from the LLM for a standard DM prompt.
    /// </summary>
    public class DmResponse
    {
        /// <summary>
        /// The narrative text to be displayed directly to the user.
        /// </summary>
        [JsonPropertyName("userFacingText")]
        [JsonConverter(typeof(SanitizedStringConverter))]
        public string UserFacingText { get; set; } = string.Empty;

        /// <summary>
        /// Optional list of hooks for creating new entities (NPCs, Locations, Quests, etc.).
        /// </summary>
        [JsonPropertyName("newEntities")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<ICreationHook>? NewEntities { get; set; }

        /// <summary>
        /// Optional structure for partial updates to apply to existing entities.
        /// Includes player, world, and arrays of NPC and location updates.
        /// </summary>
        [JsonPropertyName("partialUpdates")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonConverter(typeof(PartialUpdatesConverter))]
        public PartialUpdates? PartialUpdates { get; set; }

        /// <summary>
        /// Optional flag indicating that combat should be initiated. Defaults to false.
        /// </summary>
        [JsonPropertyName("combatTriggered")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool CombatTriggered { get; set; } = false;

        /// <summary>
        /// Optional ID of the entity (usually NPC) triggering combat.
        /// Required if combatTriggered is true to identify the opponent.
        /// </summary>
        [JsonPropertyName("enemyToEngageId")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? EnemyToEngageId { get; set; }
    }
} 