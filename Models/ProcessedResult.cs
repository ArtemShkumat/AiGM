using System.Text.Json.Serialization;

namespace AiGMBackEnd.Models
{
    public class ProcessedResult
    {
        [JsonPropertyName("userFacingText")]
        public string UserFacingText { get; set; } = string.Empty;
        
        [JsonPropertyName("success")]
        public bool Success { get; set; }
        
        [JsonPropertyName("errorMessage")]
        public string ErrorMessage { get; set; } = string.Empty;
        
        [JsonPropertyName("combatInitiated")]
        public bool CombatInitiated { get; set; } = false;

        [JsonPropertyName("combatPending")]
        public bool CombatPending { get; set; } = false;
    }
}
