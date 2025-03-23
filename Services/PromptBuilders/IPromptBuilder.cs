namespace AiGMBackEnd.Services.PromptBuilders
{
    public interface IPromptBuilder
    {
        Task<string> BuildPromptAsync(string userId, string userInput);
    }
} 