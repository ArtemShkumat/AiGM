using System.Text.Json.Serialization;

namespace AiGMBackEnd.Models
{
    public class ProcessedResult
    {
        [JsonPropertyName("userFacingText")]
        public string UserFacingText { get; set; }
        
        [JsonPropertyName("success")]
        public bool Success { get; set; }
        
        [JsonPropertyName("errorMessage")]
        public string ErrorMessage { get; set; }
    }
}
