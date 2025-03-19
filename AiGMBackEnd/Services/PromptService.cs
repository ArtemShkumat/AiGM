using System;
using System.IO;
using System.Threading.Tasks;

namespace AiGMBackEnd.Services
{
    public class PromptService
    {
        private readonly StorageService _storageService;
        private readonly LoggingService _loggingService;
        private readonly string _promptTemplatesPath;

        public PromptService(
            StorageService storageService,
            LoggingService loggingService)
        {
            _storageService = storageService;
            _loggingService = loggingService;
            _promptTemplatesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PromptTemplates");
        }

        public async Task<string> BuildPromptAsync(string promptType, string userId, string userInput)
        {
            try
            {
                // TODO: Implement prompt building logic
                // 1. Load appropriate template based on promptType
                // 2. Merge relevant game data (world, player, location, NPCs)
                // 3. Return final prompt

                return "Not implemented yet";
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error building prompt: {ex.Message}");
                throw;
            }
        }

        private async Task<string> LoadTemplateAsync(string templateName)
        {
            var templatePath = Path.Combine(_promptTemplatesPath, templateName);
            if (!File.Exists(templatePath))
            {
                throw new FileNotFoundException($"Template file not found: {templatePath}");
            }

            return await File.ReadAllTextAsync(templatePath);
        }
    }
}
