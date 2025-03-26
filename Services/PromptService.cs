using AiGMBackEnd.Models;
using AiGMBackEnd.Services.PromptBuilders;
using System.Text;
using System;
using System.Linq;

namespace AiGMBackEnd.Services
{
    public enum PromptType
    {
        DM,
        NPC,
        CreateQuest,
        CreateNPC,
        CreateLocation,
        CreatePlayerJson
    }

    public class PromptService
    {
        private readonly StorageService _storageService;
        private readonly LoggingService _loggingService;
        private readonly Dictionary<PromptType, IPromptBuilder> _promptBuilders;

        public PromptService(
            StorageService storageService,
            LoggingService loggingService)
        {
            _storageService = storageService;
            _loggingService = loggingService;
            
            // Initialize prompt builders
            _promptBuilders = new Dictionary<PromptType, IPromptBuilder>
            {
                { PromptType.DM, new DMPromptBuilder(storageService, loggingService) },
                { PromptType.NPC, new NPCPromptBuilder(storageService, loggingService) },
                { PromptType.CreateQuest, new CreateQuestPromptBuilder(storageService, loggingService) },
                { PromptType.CreateNPC, new CreateNPCPromptBuilder(storageService, loggingService) },
                { PromptType.CreateLocation, new CreateLocationPromptBuilder(storageService, loggingService) },
                { PromptType.CreatePlayerJson, new CreatePlayerJsonPromptBuilder(storageService, loggingService) }
            };
        }

        public async Task<Prompt> BuildPromptAsync(PromptType promptType, string userId, string userInput, string npcId = null, string locationType = null)
        {
            try
            {
                // Check if we have a builder for this prompt type
                if (_promptBuilders.TryGetValue(promptType, out var builder))
                {
                    // For NPC prompt types, use the dedicated NPC prompt builder with NpcId
                    if (promptType == PromptType.NPC && builder is NPCPromptBuilder npcBuilder)
                    {
                        return await npcBuilder.BuildPromptAsync(userId, userInput, npcId);
                    }
                    
                    // For CreateLocation prompt types, use locationType parameter
                    if (promptType == PromptType.CreateLocation && builder is CreateLocationPromptBuilder locationBuilder)
                    {
                        return await locationBuilder.BuildPromptAsync(userId, userInput, locationType);
                    }
                    
                    return await builder.BuildPromptAsync(userId, userInput);
                }
                
                // If we get here, we don't have a builder for the requested prompt type
                throw new ArgumentException($"Unsupported prompt type: {promptType}");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error building prompt: {ex.Message}");
                throw;
            }
        }
    }
}
