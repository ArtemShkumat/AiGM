using System.Collections.Generic;
using System.Threading.Tasks;
using static AiGMBackEnd.Services.StorageService;

namespace AiGMBackEnd.Services.Storage
{
    public interface IValidationService
    {
        Task<List<DanglingReferenceInfo>> FindDanglingReferencesAsync(string userId);
    }
} 