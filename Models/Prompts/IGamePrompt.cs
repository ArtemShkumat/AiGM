using System.Collections.Generic;

namespace AiGMBackEnd.Models.Prompts
{
    public interface IGamePrompt
    {
        string PromptBody { get; set; }
        string Model { get; set; }
        Dictionary<string, object> Context { get; set; }
    }
}

