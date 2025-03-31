using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AiGMBackEnd.Models;

namespace AiGMBackEnd.Services.Storage
{
    public class ConversationLogService
    {
        private readonly LoggingService _loggingService;
        private readonly BaseStorageService _baseStorageService;
        private readonly EntityStorageService _entityStorageService;

        public ConversationLogService(
            LoggingService loggingService, 
            BaseStorageService baseStorageService,
            EntityStorageService entityStorageService)
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
            
            log.Messages.Add(new Message
            {
                Sender = "user",
                Content = content
            });
            
            await _baseStorageService.SaveAsync(userId, "conversationLog", log);
        }

        public async Task AddDmMessageAsync(string userId, string content)
        {
            var log = await GetConversationLogAsync(userId);
            
            log.Messages.Add(new Message
            {
                Sender = "dm",
                Content = content
            });
            
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
            
            var messageEntry = new Dictionary<string, string>
            {
                { "timestamp", DateTime.UtcNow.ToString("o") },
                { "sender", "user" },
                { "content", content }
            };
            
            npc.ConversationLog.Add(messageEntry);
            
            await _baseStorageService.SaveAsync(userId, $"npcs/{npcId}", npc);
        }

        public async Task AddDmMessageToNpcLogAsync(string userId, string npcId, string content)
        {
            var npc = await _entityStorageService.GetNpcAsync(userId, npcId);
            
            if (npc == null)
            {
                _loggingService.LogWarning($"Attempted to add DM message to non-existent NPC log: {npcId}");
                return;
            }
            
            var messageEntry = new Dictionary<string, string>
            {
                { "timestamp", DateTime.UtcNow.ToString("o") },
                { "sender", npcId },
                { "content", content }
            };
            
            npc.ConversationLog.Add(messageEntry);
            
            await _baseStorageService.SaveAsync(userId, $"npcs/{npcId}", npc);
        }
    }
} 