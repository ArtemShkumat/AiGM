using System.Threading.Tasks;
using AiGMBackEnd.Models;

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
        /// <returns>The generated completion text</returns>
        Task<string> GetCompletionAsync(Prompt prompt);
    }
}
