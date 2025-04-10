using System.Collections.Generic;

namespace AiGMBackEnd.Models
{
    /// <summary>
    /// Represents the runtime state of an active combat encounter.
    /// This object is persisted between combat turns.
    /// </summary>
    public class CombatState
    {
        /// <summary>
        /// Unique identifier for this specific combat session.
        /// </summary>
        public string CombatId { get; set; } = string.Empty;

        /// <summary>
        /// The ID of the user involved in the combat.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// The ID of the EnemyStatBlock representing the opponent.
        /// </summary>
        public string EnemyStatBlockId { get; set; } = string.Empty;

        /// <summary>
        /// The current number of successful hits landed on the enemy.
        /// </summary>
        public int CurrentEnemySuccesses { get; set; } = 0;

        /// <summary>
        /// List of conditions currently affecting the player (e.g., "Minor:Winded", "Moderate:Bleeding").
        /// </summary>
        public List<string> PlayerConditions { get; set; } = new List<string>();

        /// <summary>
        /// Accumulates the narrative text (`userFacingText`) from each combat turn.
        /// Used for generating the final combat summary.
        /// </summary>
        public List<string> CombatLog { get; set; } = new List<string>();

        /// <summary>
        /// Flag indicating if the combat is currently active.
        /// Set to false when combat ends.
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
} 