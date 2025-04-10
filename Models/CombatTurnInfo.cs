using System.Collections.Generic;

namespace AiGMBackEnd.Models
{
    /// <summary>
    /// Information sent to the frontend during combat turn updates
    /// </summary>
    public class CombatTurnInfo
    {
        /// <summary>
        /// Unique identifier for the combat session
        /// </summary>
        public string CombatId { get; set; } = string.Empty;
        
        /// <summary>
        /// Current number of successful hits on the enemy
        /// </summary>
        public int CurrentEnemySuccesses { get; set; } = 0;
        
        /// <summary>
        /// Total number of successes required to defeat the enemy
        /// </summary>
        public int SuccessesRequired { get; set; } = 1;
        
        /// <summary>
        /// List of all current player conditions
        /// </summary>
        public List<string> PlayerConditions { get; set; } = new List<string>();
    }
} 