using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Collections.Generic;
using AiGMBackEnd.Models;
using AiGMBackEnd.Models.Locations;
using AiGMBackEnd.Services.Storage;

namespace AiGMBackEnd.Services
{
    public class StorageService
    {
        private readonly IBaseStorageService _baseStorageService;
        private readonly IEntityStorageService _entityStorageService;
        private readonly ITemplateService _templateService;
        private readonly IValidationService _validationService;
        private readonly IGameScenarioService _gameScenarioService;
        private readonly IConversationLogService _conversationLogService;
        private readonly LoggingService _loggingService;
        private readonly IWorldSyncService _worldSyncService;

        public StorageService(
            IBaseStorageService baseStorageService,
            IEntityStorageService entityStorageService,
            ITemplateService templateService,
            IValidationService validationService,
            IGameScenarioService gameScenarioService,
            IConversationLogService conversationLogService,
            IWorldSyncService worldSyncService,
            LoggingService loggingService)
        {
            _baseStorageService = baseStorageService;
            _entityStorageService = entityStorageService;
            _templateService = templateService;
            _validationService = validationService;
            _gameScenarioService = gameScenarioService;
            _conversationLogService = conversationLogService;
            _worldSyncService = worldSyncService;
            _loggingService = loggingService;
        }

        // Move NpcInfo and DanglingReferenceInfo classes definitions here
        // Class to use in the StorageService methods
        public class NpcInfo
        {
            public string Id { get; set; }
            public string Name { get; set; }
        }

        // Class to hold information about a dangling reference
        public class DanglingReferenceInfo
        {
            public string ReferenceId { get; set; }
            public string FilePath { get; set; }
            public string ReferenceType { get; set; }
        }

        #region Base Storage Operations

        public async Task<T> LoadAsync<T>(string userId, string fileId) where T : class => 
            await _baseStorageService.LoadAsync<T>(userId, fileId);

        public async Task SaveAsync<T>(string userId, string fileId, T entity) where T : class => 
            await _baseStorageService.SaveAsync(userId, fileId, entity);

        public async Task ApplyPartialUpdateAsync(string userId, string fileId, string jsonPatch) => 
            await _baseStorageService.ApplyPartialUpdateAsync(userId, fileId, jsonPatch);

        #endregion

        #region Entity Operations

        // Player accessors
        public async Task<Player> GetPlayerAsync(string userId) => 
            await _entityStorageService.GetPlayerAsync(userId);

        // World accessors
        public async Task<World> GetWorldAsync(string userId) => 
            await _entityStorageService.GetWorldAsync(userId);

        // Game Setting accessors
        public async Task<GameSetting> GetGameSettingAsync(string userId) => 
            await _entityStorageService.GetGameSettingAsync(userId);

        // Game Preferences accessors
        public async Task<GamePreferences> GetGamePreferencesAsync(string userId) => 
            await _entityStorageService.GetGamePreferencesAsync(userId);

        // Location accessors
        public async Task<Location> GetLocationAsync(string userId, string locationId) => 
            await _entityStorageService.GetLocationAsync(userId, locationId);

        // NPC accessors
        public async Task<Npc> GetNpcAsync(string userId, string npcId) => 
            await _entityStorageService.GetNpcAsync(userId, npcId);

        // Quest accessors
        public async Task<Quest> GetQuestAsync(string userId, string questId) => 
            await _entityStorageService.GetQuestAsync(userId, questId);

        public async Task<List<Npc>> GetNpcsInLocationAsync(string userId, string locationId) => 
            await _entityStorageService.GetNpcsInLocationAsync(userId, locationId);
        
        public async Task<List<Npc>> GetAllNpcsAsync(string gameId) => 
            await _entityStorageService.GetAllNpcsAsync(gameId);
        
        public async Task<List<Npc>> GetAllVisibleNpcsAsync(string gameId) => 
            await _entityStorageService.GetAllVisibleNpcsAsync(gameId);

        public async Task<List<NpcInfo>> GetVisibleNpcsInLocationAsync(string gameId, string locationId) => 
            await _entityStorageService.GetVisibleNpcsInLocationAsync(gameId, locationId);
        
        public async Task<List<Quest>> GetActiveQuestsAsync(string userId, List<string> activeQuestIds) => 
            await _entityStorageService.GetActiveQuestsAsync(userId, activeQuestIds);

        public async Task AddEntityToWorldAsync(string userId, string entityId, string entityName, string entityType) => 
            await _entityStorageService.AddEntityToWorldAsync(userId, entityId, entityName, entityType);

        #endregion

        #region Template Operations

        public async Task<string> GetTemplateAsync(string templatePath) => 
            await _templateService.GetTemplateAsync(templatePath);

        public async Task<string> GetDmTemplateAsync(string templateName) => 
            await _templateService.GetDmTemplateAsync(templateName);

        public async Task<string> GetNpcTemplateAsync(string templateName) => 
            await _templateService.GetNpcTemplateAsync(templateName);

        public async Task<string> GetCreateQuestTemplateAsync(string templateName) => 
            await _templateService.GetCreateQuestTemplateAsync(templateName);

        public async Task<string> GetCreateNpcTemplateAsync(string templateName) => 
            await _templateService.GetCreateNpcTemplateAsync(templateName);

        public async Task<string> GetCreateLocationTemplateAsync(string templateName, string locationType = null) => 
            await _templateService.GetCreateLocationTemplateAsync(templateName, locationType);

        public async Task<string> GetCreatePlayerJsonTemplateAsync(string templateName) => 
            await _templateService.GetCreatePlayerJsonTemplateAsync(templateName);

        #endregion

        #region Game Scenario Operations

        public List<string> GetScenarioIds() => 
            _gameScenarioService.GetScenarioIds();
        
        public async Task<T> LoadScenarioSettingAsync<T>(string scenarioId, string fileId) where T : class => 
            await _gameScenarioService.LoadScenarioSettingAsync<T>(scenarioId, fileId);
        
        public async Task<string> CreateGameFromScenarioAsync(string scenarioId, GamePreferences preferences = null) => 
            await _gameScenarioService.CreateGameFromScenarioAsync(scenarioId, preferences);
        
        public List<string> GetGameIds() => 
            _gameScenarioService.GetGameIds();

        #endregion

        #region Conversation Log Operations

        public async Task<ConversationLog> GetConversationLogAsync(string userId) => 
            await _conversationLogService.GetConversationLogAsync(userId);

        public async Task AddUserMessageAsync(string userId, string content) => 
            await _conversationLogService.AddUserMessageAsync(userId, content);

        public async Task AddDmMessageAsync(string userId, string content) => 
            await _conversationLogService.AddDmMessageAsync(userId, content);

        public async Task AddUserMessageToNpcLogAsync(string userId, string npcId, string content) => 
            await _conversationLogService.AddUserMessageToNpcLogAsync(userId, npcId, content);

        public async Task AddDmMessageToNpcLogAsync(string userId, string npcId, string content) => 
            await _conversationLogService.AddDmMessageToNpcLogAsync(userId, npcId, content);

        #endregion

        #region Validation Operations

        public async Task<List<DanglingReferenceInfo>> FindDanglingReferencesAsync(string userId) => 
            await _validationService.FindDanglingReferencesAsync(userId);

        #endregion

        #region World Sync Operations

        /// <summary>
        /// Synchronizes the world.json file with all existing entities in the game directory structure.
        /// This method scans the NPCs, locations, quests, and lore folders and updates the world.json file accordingly.
        /// </summary>
        /// <param name="userId">The user/game ID</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task SyncWorldWithEntitiesAsync(string userId) => 
            await _worldSyncService.SyncWorldWithEntitiesAsync(userId);

        #endregion
    }
}
