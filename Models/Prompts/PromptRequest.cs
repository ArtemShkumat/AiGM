using System;
using AiGMBackEnd.Services;
using System.Collections.Generic;

namespace AiGMBackEnd.Models.Prompts
{
    public class PromptRequest
    {
        public PromptType PromptType { get; set; } = PromptType.DM;
        public string UserId { get; set; } = string.Empty;
        public string UserInput { get; set; } = string.Empty;
        public string NpcId { get; set; } = string.Empty;
        public string NpcName { get; set; } = string.Empty;
        public string NpcLocation { get; set; } = string.Empty;
        public string LocationType { get; set; } = string.Empty;
        public string LocationId { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public string ParentLocationId { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;
        public string QuestName { get; set; } = string.Empty;
        public string QuestId { get; set; } = string.Empty;
        public string ScenarioId { get; set; } = string.Empty;
        
        // Dictionary for additional metadata that doesn't fit into the standard properties
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }
} 