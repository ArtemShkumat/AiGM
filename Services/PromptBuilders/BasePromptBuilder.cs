using AiGMBackEnd.Models;
using AiGMBackEnd.Services;
using System.Text;
using System;

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
        
        public virtual Task<Prompt> BuildPromptAsync(string userId, string userInput, string typeParameter = null)
        {
            // Default implementation calls the method without typeParameter
            return BuildPromptAsync(userId, userInput);
        }
        
        public abstract Task<Prompt> BuildPromptAsync(string userId, string userInput);
        
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