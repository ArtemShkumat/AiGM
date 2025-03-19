using System;
using System.Threading.Tasks;
using AiGMBackEnd.Services.AIProviders;
using Microsoft.Extensions.Configuration;

namespace AiGMBackEnd.Services
{
    public class AiService
    {
        private readonly AIProviderFactory _providerFactory;
        private readonly LoggingService _loggingService;
        private readonly IConfiguration _configuration;
        private readonly string _defaultProvider;

        public AiService(
            AIProviderFactory providerFactory,
            IConfiguration configuration,
            LoggingService loggingService)
        {
            _providerFactory = providerFactory;
            _configuration = configuration;
            _loggingService = loggingService;
            
            // Get default provider from configuration or use OpenAI
            _defaultProvider = configuration["AIProviders:DefaultProvider"] ?? "OpenAI";
        }

        public async Task<string> GetCompletionAsync(string prompt, PromptType promptType)
        {
            try
            {
                _loggingService.LogInfo($"Requesting completion for {promptType} prompt");
                
                // Create the provider (will use default if not specified)
                var provider = _providerFactory.CreateProvider(_defaultProvider);
                
                // Get completion from provider
                var response = await provider.GetCompletionAsync(prompt, promptType.ToString());
                
                _loggingService.LogInfo($"Received completion response from {provider.Name}");
                
                return response;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error getting completion: {ex.Message}");
                throw;
            }
        }
    }
}
