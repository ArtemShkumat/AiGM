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
        private readonly IWorldSyncService _worldSyncService;
        private readonly IRecentEventsService _recentEventsService;
        private readonly IEnemyStatBlockService _enemyStatBlockService;
        private readonly ICombatStateService _combatStateService;
        private readonly LoggingService _loggingService;

        public StorageService(
            IBaseStorageService baseStorageService,
            IEntityStorageService entityStorageService,
            ITemplateService templateService,
            IValidationService validationService,
            IGameScenarioService gameScenarioService,
            IConversationLogService conversationLogService,
            IWorldSyncService worldSyncService,
            IRecentEventsService recentEventsService,
            IEnemyStatBlockService enemyStatBlockService,
            ICombatStateService combatStateService,
            LoggingService loggingService)
        {
            _baseStorageService = baseStorageService;
            _entityStorageService = entityStorageService;
            _templateService = templateService;
            _validationService = validationService;
            _gameScenarioService = gameScenarioService;
            _conversationLogService = conversationLogService;
            _worldSyncService = worldSyncService;
            _recentEventsService = recentEventsService;
            _enemyStatBlockService = enemyStatBlockService;
            _combatStateService = combatStateService;
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
        public async Task<World> GetWorldAsync(string userId, string scenarioId = null) => 
            await _entityStorageService.GetWorldAsync(userId, scenarioId);

        // Game Setting accessors
        public async Task<GameSetting> GetGameSettingAsync(string userId, string scenarioId = null) => 
            await _entityStorageService.GetGameSettingAsync(userId, scenarioId);

        // Game Preferences accessors
        public async Task<GamePreferences> GetGamePreferencesAsync(string userId) => 
            await _entityStorageService.GetGamePreferencesAsync(userId);

        // Location accessors
        public async Task<Location> GetLocationAsync(string userId, string locationId, string scenarioId = null) => 
            await _entityStorageService.GetLocationAsync(userId, locationId, scenarioId);

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
       
        public async Task<List<Quest>> GetActiveQuestsAsync(string userId, List<string> activeQuestIds) => 
            await _entityStorageService.GetActiveQuestsAsync(userId, activeQuestIds);

        public async Task<List<Quest>> GetAllQuestsAsync(string gameId) => 
            await _entityStorageService.GetAllQuestsAsync(gameId);

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

        public async Task<string> GetCreatePlayerTemplateAsync(string templateName) => 
            await _templateService.GetCreatePlayerTemplateAsync(templateName);

        public async Task<string> GetSummarizeTemplateAsync(string templateName) => 
            await _templateService.GetSummarizeTemplateAsync(templateName);

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
        /// This method scans the NPCs, locations, quests, and folders and updates the world.json file accordingly.
        /// </summary>
        /// <param name="userId">The user/game ID</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task SyncWorldWithEntitiesAsync(string userId) => 
            await _worldSyncService.SyncWorldWithEntitiesAsync(userId);        

        #endregion

        #region Recent Events Operations

        /// <summary>
        /// Loads the recent events summary for a user.
        /// </summary>
        /// <param name="userId">The user/game ID.</param>
        /// <returns>The RecentEvents object, or null if not found.</returns>
        public async Task<RecentEvents> GetRecentEventsAsync(string userId) =>
            await _recentEventsService.GetRecentEventsAsync(userId);
        
        // Optionally, keep a method to add summaries directly if needed elsewhere,
        // but it's better handled by the processor via the service.
        // public async Task AddSummaryToRecentEventsAsync(string userId, string summary) =>
        //     await _recentEventsService.AddSummaryToRecentEventsAsync(userId, summary);

        #endregion

        #region Enemy Stat Block Operations

        /// <summary>
        /// Loads an EnemyStatBlock from its JSON file.
        /// </summary>
        /// <param name="userId">The user/game ID.</param>
        /// <param name="enemyId">The ID of the enemy (e.g., "npc_goblin1" or "enemy_goblin1").</param>
        /// <returns>The EnemyStatBlock object, or null if not found or on error.</returns>
        public async Task<EnemyStatBlock?> LoadEnemyStatBlockAsync(string userId, string enemyId) =>
            await _enemyStatBlockService.LoadEnemyStatBlockAsync(userId, enemyId);

        /// <summary>
        /// Saves an EnemyStatBlock to its JSON file, calculating SuccessesRequired.
        /// </summary>
        /// <param name="userId">The user/game ID.</param>
        /// <param name="statBlock">The EnemyStatBlock object to save. Its ID will be used for the filename.</param>
        public async Task SaveEnemyStatBlockAsync(string userId, EnemyStatBlock statBlock) =>
            await _enemyStatBlockService.SaveEnemyStatBlockAsync(userId, statBlock);

        /// <summary>
        /// Checks if an EnemyStatBlock file exists.
        /// </summary>
        /// <param name="userId">The user/game ID.</param>
        /// <param name="enemyId">The ID of the enemy (e.g., "npc_goblin1" or "enemy_goblin1").</param>
        /// <returns>True if the file exists, false otherwise.</returns>
        public Task<bool> CheckIfStatBlockExistsAsync(string userId, string enemyId) =>
            _enemyStatBlockService.CheckIfStatBlockExistsAsync(userId, enemyId);

        #endregion

        #region Combat Operations

        /// <summary>
        /// Saves a CombatState to a JSON file.
        /// </summary>
        /// <param name="userId">The user/game ID.</param>
        /// <param name="combatState">The CombatState object to save.</param>
        public async Task SaveCombatStateAsync(string userId, CombatState combatState) =>
            await _combatStateService.SaveCombatStateAsync(userId, combatState);

        /// <summary>
        /// Loads the active CombatState for a user.
        /// </summary>
        /// <param name="userId">The user/game ID.</param>
        /// <returns>The CombatState object, or null if not found or on error.</returns>
        public async Task<CombatState?> LoadCombatStateAsync(string userId) =>
            await _combatStateService.LoadCombatStateAsync(userId);

        /// <summary>
        /// Deletes the CombatState file for a user.
        /// </summary>
        /// <param name="userId">The user/game ID.</param>
        public async Task DeleteCombatStateAsync(string userId) =>
            await _combatStateService.DeleteCombatStateAsync(userId);

        #endregion

        // Add this method to the StorageService class
        public async Task<string> GetCreateScenarioTemplateAsync(string fileName)
        {
            try
            {
                string path = Path.Combine("PromptTemplates", "Create", "Scenario", fileName);
                return await File.ReadAllTextAsync(path);
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error reading create scenario template '{fileName}': {ex.Message}");
                throw;
            }
        }

        #region Scenario File Operations
        
        /// <summary>
        /// Creates the folder structure for a scenario
        /// </summary>
        /// <param name="scenarioId">The scenario ID</param>
        /// <param name="userId">The user ID, if this is a user-created scenario</param>
        /// <param name="isStartingScenario">Whether this is a starting scenario template</param>
        /// <returns>Task representing the async operation</returns>
        public async Task CreateScenarioFolderStructureAsync(string scenarioId, string userId, bool isStartingScenario) =>
            await _gameScenarioService.CreateScenarioFolderStructureAsync(scenarioId, userId, isStartingScenario);
        
        /// <summary>
        /// Saves a JSON file for a scenario
        /// </summary>
        /// <param name="scenarioId">The scenario ID</param>
        /// <param name="fileName">The file name to save</param>
        /// <param name="jsonData">The JSON data to save</param>
        /// <param name="userId">The user ID, if this is a user-created scenario</param>
        /// <param name="isStartingScenario">Whether this is a starting scenario template</param>
        /// <returns>Task representing the async operation</returns>
        public async Task SaveScenarioFileAsync(string scenarioId, string fileName, JToken jsonData, string userId, bool isStartingScenario) =>
            await _gameScenarioService.SaveScenarioFileAsync(scenarioId, fileName, jsonData, userId, isStartingScenario);
        
        /// <summary>
        /// Saves a location file for a scenario
        /// </summary>
        /// <param name="scenarioId">The scenario ID</param>
        /// <param name="locationId">The location ID</param>
        /// <param name="locationData">The location data to save</param>
        /// <param name="userId">The user ID, if this is a user-created scenario</param>
        /// <param name="isStartingScenario">Whether this is a starting scenario template</param>
        /// <returns>Task representing the async operation</returns>
        public async Task SaveScenarioLocationAsync(string scenarioId, string locationId, JToken locationData, string userId, bool isStartingScenario) =>
            await _gameScenarioService.SaveScenarioLocationAsync(scenarioId, locationId, locationData, userId, isStartingScenario);
        
        /// <summary>
        /// Saves an NPC file for a scenario
        /// </summary>
        /// <param name="scenarioId">The scenario ID</param>
        /// <param name="npcId">The NPC ID</param>
        /// <param name="npcData">The NPC data to save</param>
        /// <param name="userId">The user ID, if this is a user-created scenario</param>
        /// <param name="isStartingScenario">Whether this is a starting scenario template</param>
        /// <returns>Task representing the async operation</returns>
        public async Task SaveScenarioNpcAsync(string scenarioId, string npcId, JToken npcData, string userId, bool isStartingScenario) =>
            await _gameScenarioService.SaveScenarioNpcAsync(scenarioId, npcId, npcData, userId, isStartingScenario);
        
        #endregion
    }
}
