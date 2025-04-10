using AiGMBackEnd.Models;
using AiGMBackEnd.Models.Prompts;
using AiGMBackEnd.Services;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System;

namespace AiGMBackEnd.Services.PromptBuilders
{
    public abstract class BasePromptBuilder : IPromptBuilder
    {
        protected readonly StorageService _storageService;
        protected readonly LoggingService _loggingService;
        
        // JSON options for serializing context objects for the prompt
        private static readonly JsonSerializerOptions _promptSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true, // Makes the JSON readable in the prompt log
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, // Exclude null properties
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase // Match typical JSON conventions
        };

        protected BasePromptBuilder(StorageService storageService, LoggingService loggingService)
        {
            _storageService = storageService;
            _loggingService = loggingService;
        }
        
        public abstract Task<Prompt> BuildPromptAsync(PromptRequest request);
        
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

        /// <summary>
        /// Serializes an object into a JSON string suitable for inclusion in an LLM prompt.
        /// </summary>
        protected string SerializeForPrompt(object obj)
        {
            if (obj == null)
            {
                return "null";
            }
            try
            {
                return JsonSerializer.Serialize(obj, _promptSerializerOptions);
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error serializing object of type {obj.GetType().Name} for prompt: {ex.Message}");
                return "{ \"error\": \"Could not serialize object for prompt.\" }"; // Return error JSON
            }
        }
    }
} 