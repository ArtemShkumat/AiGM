using System.Threading.Tasks;

namespace AiGMBackEnd.Services.Storage
{
    public interface ITemplateService
    {
        Task<string> GetTemplateAsync(string templatePath);
        Task<string> GetDmTemplateAsync(string templateName);
        Task<string> GetNpcTemplateAsync(string templateName);
        Task<string> GetCreateQuestTemplateAsync(string templateName);
        Task<string> GetCreateNpcTemplateAsync(string templateName);
        Task<string> GetCreateLocationTemplateAsync(string templateName, string locationType = null);
        Task<string> GetCreatePlayerJsonTemplateAsync(string templateName);
        Task<string> GetSummarizeTemplateAsync(string templateName);
    }
} 