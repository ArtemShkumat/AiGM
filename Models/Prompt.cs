using AiGMBackEnd.Services;

namespace AiGMBackEnd.Models
{
    public class Prompt
    {
        public string SystemPrompt { get; set; }
        public string PromptContent { get; set; }
        public PromptType PromptType { get; set; }

        public Prompt(string systemPrompt, string promptContent, PromptType promptType)
        {
            SystemPrompt = systemPrompt;
            PromptContent = promptContent;
            PromptType = promptType;
        }
    }
} 