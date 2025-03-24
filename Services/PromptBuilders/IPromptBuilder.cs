using AiGMBackEnd.Models;

namespace AiGMBackEnd.Services.PromptBuilders
{
    public interface IPromptBuilder
    {
        Task<Prompt> BuildPromptAsync(string userId, string userInput);
    }
} 