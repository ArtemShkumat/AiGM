using System.Collections.Generic;
using System.Text.Json.Serialization;
using AiGMBackEnd.Models; // Needed for converters and interfaces

namespace AiGMBackEnd.Models
{
    /// <summary>
    /// Represents the strongly-typed structured response expected from the DM prompt.
    /// </summary>
    public class DmResponse
    {
        /// <summary>
        /// The narrative text to be displayed to the player.
        /// </summary>
        [JsonPropertyName("userFacingText")]
        public string UserFacingText { get; set; }

        /// <summary>
        /// List of new entities (NPCs, Locations, Quests) to be created.
        /// Deserialized using CreationHookListConverter.
        /// </summary>
        [JsonPropertyName("newEntities")]
        [JsonConverter(typeof(CreationHookListConverter))] // Use the list converter
        public List<ICreationHook> NewEntities { get; set; } = new List<ICreationHook>();

        /// <summary>
        /// Dictionary containing partial updates for existing entities.
        /// Keys are entity IDs (e.g., "player", "npc_id_1", "loc_id_2").
        /// Values are specific IUpdatePayload implementations, deserialized using UpdatePayloadConverter/DictionaryConverter.
        /// </summary>
        [JsonPropertyName("partialUpdates")]
        [JsonConverter(typeof(UpdatePayloadDictionaryConverter))] // Apply converter to the dictionary
        public Dictionary<string, IUpdatePayload> PartialUpdates { get; set; } = new Dictionary<string, IUpdatePayload>();
    }
} 