using AiGMBackEnd.Models;
using System.Threading.Tasks;

namespace AiGMBackEnd.Services.Storage.Interfaces
{
    public interface IScenarioTemplateStorageService
    {
        Task SaveTemplateAsync(ScenarioTemplate template);
        Task<ScenarioTemplate> LoadTemplateAsync(string templateId);
        string GetTemplateFilePath(string templateId);
    }
} 