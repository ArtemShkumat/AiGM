using AiGMBackEnd.Models;
using AiGMBackEnd.Models.Prompts;

namespace AiGMBackEnd.Services.PromptBuilders
{
    public interface IPromptBuilder
    {
        Task<Prompt> BuildPromptAsync(PromptRequest request);
    }
} 