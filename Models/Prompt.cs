using AiGMBackEnd.Services;

namespace AiGMBackEnd.Models
{
    public class Prompt
    {
        public string SystemPrompt { get; set; }
        public string PromptContent { get; set; }
        public PromptType PromptType { get; set; }
        public string? OutputStructureJsonSchema { get; set; }

        public Prompt(string systemPrompt, string promptContent, PromptType promptType, string? outputStructureJsonSchema = null)
        {
            SystemPrompt = systemPrompt ?? throw new ArgumentNullException(nameof(systemPrompt));
            PromptContent = promptContent ?? throw new ArgumentNullException(nameof(promptContent));
            PromptType = promptType;
            OutputStructureJsonSchema = outputStructureJsonSchema;
        }
    }
} 