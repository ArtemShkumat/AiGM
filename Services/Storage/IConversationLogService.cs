using System.Threading.Tasks;
using AiGMBackEnd.Models;

namespace AiGMBackEnd.Services.Storage
{
    public interface IConversationLogService
    {
        Task<ConversationLog> GetConversationLogAsync(string userId);
        Task AddUserMessageAsync(string userId, string content);
        Task AddDmMessageAsync(string userId, string content);
        Task AddUserMessageToNpcLogAsync(string userId, string npcId, string content);
        Task AddDmMessageToNpcLogAsync(string userId, string npcId, string content);
    }
} 