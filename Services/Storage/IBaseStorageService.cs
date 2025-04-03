using System.Threading.Tasks;

namespace AiGMBackEnd.Services.Storage
{
    public interface IBaseStorageService
    {
        Task<T> LoadAsync<T>(string userId, string fileId) where T : class;
        Task SaveAsync<T>(string userId, string fileId, T entity) where T : class;
        Task ApplyPartialUpdateAsync(string userId, string fileId, string jsonPatch);
        string GetFilePath(string userId, string fileId);
        void CopyDirectory(string sourceDir, string destinationDir);
    }
} 