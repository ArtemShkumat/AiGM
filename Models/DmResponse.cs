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
        public string UserFacingText { get; set; } = string.Empty;

        /// <summary>
        /// Optional list of hooks for creating new entities (NPCs, Locations, Quests, etc.).
        /// </summary>
        [JsonPropertyName("newEntities")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<ICreationHook>? NewEntities { get; set; }

        /// <summary>
        /// Optional dictionary of partial updates to apply to existing entities.
        /// Key is the entity ID (e.g., "npc_goblin1", "loc_tavern"), Value contains the update payload.
        /// </summary>
        [JsonPropertyName("partialUpdates")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, IUpdatePayload>? PartialUpdates { get; set; }

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