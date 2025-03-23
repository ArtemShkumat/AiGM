using AiGMBackEnd.Services;
using System.Text;

namespace AiGMBackEnd.Services.PromptBuilders
{
    public abstract class BasePromptBuilder : IPromptBuilder
    {
        protected readonly StorageService _storageService;
        protected readonly LoggingService _loggingService;
        
        protected BasePromptBuilder(StorageService storageService, LoggingService loggingService)
        {
            _storageService = storageService;
            _loggingService = loggingService;
        }
        
        public abstract Task<string> BuildPromptAsync(string userId, string userInput);
        
        protected async Task<string> GetTemplateAsync(string templateName)
        {
            try
            {
                return await _storageService.GetTemplateAsync(templateName);
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error loading template {templateName}: {ex.Message}");
                throw;
            }
        }
    }
} 