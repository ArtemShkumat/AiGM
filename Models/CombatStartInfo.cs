using System.Collections.Generic;

namespace AiGMBackEnd.Models
{
    /// <summary>
    /// Information sent to the frontend when combat starts
    /// </summary>
    public class CombatStartInfo
    {
        /// <summary>
        /// Unique identifier for the combat session
        /// </summary>
        public string CombatId { get; set; } = string.Empty;
        
        /// <summary>
        /// ID of the enemy being fought
        /// </summary>
        public string EnemyId { get; set; } = string.Empty;
        
        /// <summary>
        /// Name of the enemy being fought
        /// </summary>
        public string EnemyName { get; set; } = string.Empty;
        
        /// <summary>
        /// Description of the enemy being fought
        /// </summary>
        public string EnemyDescription { get; set; } = string.Empty;
        
        /// <summary>
        /// Level of the enemy (1-10)
        /// </summary>
        public int EnemyLevel { get; set; } = 1;
        
        /// <summary>
        /// Number of successful hits required to defeat the enemy
        /// </summary>
        public int SuccessesRequired { get; set; } = 1;
        
        /// <summary>
        /// List of player conditions at the start of combat (usually empty)
        /// </summary>
        public List<string> PlayerConditions { get; set; } = new List<string>();
    }
} 