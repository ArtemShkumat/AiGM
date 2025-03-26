using System;
using AiGMBackEnd.Services;

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
        public string Context { get; set; } = string.Empty;
    }
} 