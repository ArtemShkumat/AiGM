using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AiGMBackEnd.Models;
using System.Linq;

namespace AiGMBackEnd.Services.Storage
{
    public class ConversationLogService : IConversationLogService
    {
        private readonly LoggingService _loggingService;
        private readonly IBaseStorageService _baseStorageService;
        private readonly IEntityStorageService _entityStorageService;

        public ConversationLogService(
            LoggingService loggingService, 
            IBaseStorageService baseStorageService,
            IEntityStorageService entityStorageService)
        {
            _loggingService = loggingService;
            _baseStorageService = baseStorageService;
            _entityStorageService = entityStorageService;
        }

        // Conversation Log accessors
        public async Task<ConversationLog> GetConversationLogAsync(string userId)
        {
            var log = await _baseStorageService.LoadAsync<ConversationLog>(userId, "conversationLog");
            
            // If the log doesn't exist yet, create a new one
            if (log == null)
            {
                log = new ConversationLog();
                await _baseStorageService.SaveAsync(userId, "conversationLog", log);
            }
            
            return log;
        }

        public async Task AddUserMessageAsync(string userId, string content)
        {
            var log = await GetConversationLogAsync(userId);
            
            // Create the message in the new format
            var message = new Dictionary<string, string>
            {
                { "Player to GM", content }
            };
            
            log.Messages.Add(message);
            
            await _baseStorageService.SaveAsync(userId, "conversationLog", log);
        }

        public async Task AddDmMessageAsync(string userId, string content)
        {
            var log = await GetConversationLogAsync(userId);
            
            // Create the message in the new format
            var message = new Dictionary<string, string>
            {
                { "GM to Player", content }
            };
            
            log.Messages.Add(message);
            
            await _baseStorageService.SaveAsync(userId, "conversationLog", log);
        }

        public async Task AddUserMessageToNpcLogAsync(string userId, string npcId, string content)
        {
            var npc = await _entityStorageService.GetNpcAsync(userId, npcId);
            
            if (npc == null)
            {
                _loggingService.LogWarning($"Attempted to add user message to non-existent NPC log: {npcId}");
                return;
            }
            
            // Use NPC Name if available, otherwise use ID
            string npcIdentifier = string.IsNullOrEmpty(npc.Name) ? npcId : npc.Name;
            string messageKey = $"Player to {npcIdentifier}";
            
            // Create message for NPC log
            var messageEntryNpc = new Dictionary<string, string>
            {
                { messageKey, content }
            };
            
            // Change NPC ConversationLog type to match main log
            if (npc.ConversationLog == null)
            {
                 npc.ConversationLog = new List<Dictionary<string, string>>();
            }
            npc.ConversationLog.Add(messageEntryNpc);
            await _baseStorageService.SaveAsync(userId, $"npcs/{npcId}", npc);

            // Also add to the main DM log
            var dmLog = await GetConversationLogAsync(userId);
            var messageEntryDm = new Dictionary<string, string>
            {
                { messageKey, content }
            };
            dmLog.Messages.Add(messageEntryDm);
            await _baseStorageService.SaveAsync(userId, "conversationLog", dmLog);
        }

        public async Task AddDmMessageToNpcLogAsync(string userId, string npcId, string content)
        {
            var npc = await _entityStorageService.GetNpcAsync(userId, npcId);
            
            if (npc == null)
            {
                _loggingService.LogWarning($"Attempted to add DM message to non-existent NPC log: {npcId}");
                return;
            }

            // Use NPC Name if available, otherwise use ID
            string npcIdentifier = string.IsNullOrEmpty(npc.Name) ? npcId : npc.Name;
            string messageKey = $"{npcIdentifier} to Player";
            
            // Create message for NPC log
            var messageEntryNpc = new Dictionary<string, string>
            {
                { messageKey, content }
            };

            // Change NPC ConversationLog type to match main log
            if (npc.ConversationLog == null)
            {
                 npc.ConversationLog = new List<Dictionary<string, string>>();
            }
            npc.ConversationLog.Add(messageEntryNpc);
            await _baseStorageService.SaveAsync(userId, $"npcs/{npcId}", npc);

            // Also add to the main DM log
            //var dmLog = await GetConversationLogAsync(userId);
            //var messageEntryDm = new Dictionary<string, string>
            //{
               // { messageKey, content }
            //};
            //dmLog.Messages.Add(messageEntryDm);
            //await _baseStorageService.SaveAsync(userId, "conversationLog", dmLog);
        }

        /// <summary>
        /// Wipes the conversation log for a user, keeping only the last message.
        /// </summary>
        public async Task WipeLogAsync(string userId)
        {
            try
            {
                var conversationLog = await GetConversationLogAsync(userId);
                if (conversationLog != null && conversationLog.Messages.Count > 1)
                {
                    var lastMessage = conversationLog.Messages.LastOrDefault();
                    conversationLog.Messages.Clear();
                    if (lastMessage != null)
                    {
                        conversationLog.Messages.Add(lastMessage);
                    }
                    await _baseStorageService.SaveAsync(userId, GetLogFileName(userId), conversationLog);
                    _loggingService.LogInfo($"Wiped conversation log for user {userId}, keeping last message.");
                }
                else
                {
                    _loggingService.LogInfo($"Conversation log for user {userId} has 1 or fewer messages. No wipe needed.");
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error wiping conversation log for user {userId}: {ex.Message}");
                throw;
            }
        }

        // Private helper methods
        private string GetLogFileName(string userId)
        {
            return "conversationLog";
        }
    }
} 