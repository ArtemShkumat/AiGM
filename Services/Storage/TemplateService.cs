using System;
using System.IO;
using System.Threading.Tasks;

namespace AiGMBackEnd.Services.Storage
{
    public class TemplateService : ITemplateService
    {
        private readonly LoggingService _loggingService;
        private readonly string _promptTemplatesPath;

        public TemplateService(LoggingService loggingService)
        {
            _loggingService = loggingService;
            
            // Set the templates path
            string rootDirectory = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.FullName;
            _promptTemplatesPath = Path.Combine(rootDirectory, "PromptTemplates");
        }

        public async Task<string> GetTemplateAsync(string templatePath)
        {
            var fullPath = Path.Combine(_promptTemplatesPath, templatePath);
            if (!File.Exists(fullPath))
            {
                _loggingService.LogWarning($"Template file not found: {fullPath}. Using empty template.");
                return string.Empty;
            }

            return await File.ReadAllTextAsync(fullPath);
        }

        // Specific template accessor methods for different prompt types
        public async Task<string> GetDmTemplateAsync(string templateName)
        {
            return await GetTemplateAsync($"DmPrompt/{templateName}.txt");
        }

        public async Task<string> GetNpcTemplateAsync(string templateName)
        {
            return await GetTemplateAsync($"NPCPrompt/{templateName}.txt");
        }

        public async Task<string> GetCreateQuestTemplateAsync(string templateName)
        {
            return await GetTemplateAsync($"Create/Quest/{templateName}.txt");
        }

        public async Task<string> GetCreateNpcTemplateAsync(string templateName)
        {
            return await GetTemplateAsync($"Create/NPC/{templateName}.txt");
        }

        public async Task<string> GetCreateLocationTemplateAsync(string templateName, string locationType = null)
        {
            if (string.IsNullOrEmpty(locationType))
            {
                // Default to general location template
                return await GetTemplateAsync($"Create/Location/{templateName}.txt");
            }
            else
            {
                // Use location type specific template
                return await GetTemplateAsync($"Create/Location/{locationType}/{templateName}.txt");
            }
        }

        public async Task<string> GetCreatePlayerTemplateAsync(string templateName)
        {
            return await GetTemplateAsync($"Create/Player/{templateName}");
        }

        public async Task<string> GetSummarizeTemplateAsync(string templateName)
        {
            return await GetTemplateAsync($"Summarize/{templateName}.txt");
        }
    }
} 