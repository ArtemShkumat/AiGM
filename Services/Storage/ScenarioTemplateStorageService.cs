using AiGMBackEnd.Models;
using AiGMBackEnd.Services.Storage.Interfaces;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;

namespace AiGMBackEnd.Services.Storage
{
    public class ScenarioTemplateStorageService : IScenarioTemplateStorageService
    {
        private readonly IBaseStorageService _baseStorageService;
        private readonly ILogger<ScenarioTemplateStorageService> _logger;
        private const string ADMIN_USER_ID = "admin"; // Use admin as the user ID for global scenario templates

        public ScenarioTemplateStorageService(IBaseStorageService baseStorageService, ILogger<ScenarioTemplateStorageService> logger)
        {
            _baseStorageService = baseStorageService;
            _logger = logger;
        }

        public string GetTemplateFilePath(string templateId)
        {
            return Path.Combine("scenarioTemplates", templateId, "template");
        }

        public async Task<ScenarioTemplate> LoadTemplateAsync(string templateId)
        {
            var filePath = GetTemplateFilePath(templateId);
            _logger.LogInformation($"Loading scenario template from {filePath}");
            
            return await _baseStorageService.LoadAsync<ScenarioTemplate>(ADMIN_USER_ID, filePath);
        }

        public async Task SaveTemplateAsync(ScenarioTemplate template)
        {
            if (template == null)
            {
                _logger.LogWarning("Attempted to save null ScenarioTemplate");
                return;
            }

            if (string.IsNullOrEmpty(template.TemplateId))
            {
                _logger.LogError("Cannot save ScenarioTemplate with empty TemplateId");
                throw new System.ArgumentException("TemplateId cannot be empty", nameof(template));
            }

            var filePath = GetTemplateFilePath(template.TemplateId);
            _logger.LogInformation($"Saving scenario template to {filePath}");
            
            await _baseStorageService.SaveAsync(ADMIN_USER_ID, filePath, template);
        }
    }
} 