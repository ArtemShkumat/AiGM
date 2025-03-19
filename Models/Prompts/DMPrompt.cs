using System.Collections.Generic;

namespace AiGMBackEnd.Models.Prompts
{
    public class DMPrompt : IGamePrompt
    {
        public string PromptBody { get; set; }
        public string Model { get; set; }
        public Dictionary<string, object> Context { get; set; } = new Dictionary<string, object>();
        
        // DM-specific fields can be added here
        public string PlayerCurrentLocationId { get; set; }
        public string CurrentAction { get; set; }
        public bool IsWorldCreation { get; set; }
    }
}

