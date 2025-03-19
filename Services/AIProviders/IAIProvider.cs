using System.Threading.Tasks;

namespace AiGMBackEnd.Services.AIProviders
{
    public interface IAIProvider
    {
        /// <summary>
        /// Gets the name of the AI provider
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Generates a completion from the AI model
        /// </summary>
        /// <param name="prompt">The prompt to send to the AI model</param>
        /// <param name="promptType">The type of prompt (DM, NPC, etc.)</param>
        /// <returns>The generated completion text</returns>
        Task<string> GetCompletionAsync(string prompt, string promptType);
    }
}
