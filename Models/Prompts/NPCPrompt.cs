using System.Collections.Generic;

namespace AiGMBackEnd.Models.Prompts
{
    public class NPCPrompt : IGamePrompt
    {
        public string PromptBody { get; set; }
        public string Model { get; set; }
        public Dictionary<string, object> Context { get; set; } = new Dictionary<string, object>();
        
        // NPC-specific fields
        public string NpcId { get; set; }
        public SceneContext Scene { get; set; }
    }
}

