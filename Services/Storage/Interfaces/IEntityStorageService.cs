using System.Collections.Generic;
using System.Threading.Tasks;
using AiGMBackEnd.Models;
using AiGMBackEnd.Models.Locations;

namespace AiGMBackEnd.Services.Storage
{
    public interface IEntityStorageService
    {
        Task<Player> GetPlayerAsync(string userId);
        Task<World> GetWorldAsync(string userId);
        Task<GameSetting> GetGameSettingAsync(string userId);
        Task<GamePreferences> GetGamePreferencesAsync(string userId);
        Task<Location> GetLocationAsync(string userId, string locationId);
        Task<Npc> GetNpcAsync(string userId, string npcId);
        Task<Quest> GetQuestAsync(string userId, string questId);
        Task<List<Npc>> GetNpcsInLocationAsync(string userId, string locationId);
        Task<List<Npc>> GetAllNpcsAsync(string gameId);
        Task<List<Npc>> GetAllVisibleNpcsAsync(string gameId);
        Task<List<StorageService.NpcInfo>> GetVisibleNpcsInLocationAsync(string gameId, string locationId);
        Task<List<Quest>> GetActiveQuestsAsync(string userId, List<string> activeQuestIds);
        Task<List<Quest>> GetAllQuestsAsync(string userId);
        Task AddEntityToWorldAsync(string userId, string entityId, string entityName, string entityType);
    }
} 