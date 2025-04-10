using System.Collections.Generic;

namespace AiGMBackEnd.Models
{
    /// <summary>
    /// Represents the combat statistics and information for an enemy entity.
    /// Based on the combat overview document.
    /// </summary>
    public class EnemyStatBlock
    {
        /// <summary>
        /// Unique identifier for the enemy stat block (e.g., "enemy_goblin_scout_01", often tied to an NPC ID).
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Display name of the enemy.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Enemy level (1-10), determining base difficulty to hit.
        /// </summary>
        public int Level { get; set; } = 1;

        /// <summary>
        /// Number of successful hits required to defeat the enemy (typically Level / 2, rounded up).
        /// Should be calculated and set when the stat block is created/saved.
        /// </summary>
        public int SuccessesRequired { get; set; } = 1;

        /// <summary>
        /// Narrative flavor text and visual description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Narrative description of the condition or method required for player hits to count as successes.
        /// </summary>
        public string Vulnerability { get; set; } = string.Empty;

        /// <summary>
        /// Narrative description of what happens if the enemy defeats the player.
        /// </summary>
        public string BadStuff { get; set; } = string.Empty;

        /// <summary>
        /// Optional list of tags representing enemy abilities, resistances, or special traits.
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();
    }
} 