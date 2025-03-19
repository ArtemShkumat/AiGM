using System;
using Microsoft.Extensions.Configuration;

namespace AiGMBackEnd.Services.AIProviders
{
    public class AIProviderFactory
    {
        private readonly IConfiguration _configuration;
        private readonly LoggingService _loggingService;

        public AIProviderFactory(IConfiguration configuration, LoggingService loggingService)
        {
            _configuration = configuration;
            _loggingService = loggingService;
        }

        /// <summary>
        /// Creates an AI provider instance based on the specified provider name
        /// </summary>
        /// <param name="providerName">The name of the provider to create (defaults to "OpenAI" if null)</param>
        /// <returns>An instance of the specified provider</returns>
        /// <exception cref="ArgumentException">Thrown if the specified provider is not supported</exception>
        public IAIProvider CreateProvider(string? providerName = null)
        {
            providerName ??= "OpenAI";

            _loggingService.LogInfo($"Creating AI provider: {providerName}");

            return providerName.ToLower() switch
            {
                "openai" => new OpenAIProvider(_configuration, _loggingService),
                // Add more providers here as they are implemented
                _ => throw new ArgumentException($"Unsupported AI provider: {providerName}")
            };
        }
    }
}
