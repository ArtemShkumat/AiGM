using AiGMBackEnd.Models;
using AiGMBackEnd.Models.Prompts;
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
        CreatePlayerJson,
        Summarize
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
                { PromptType.CreatePlayerJson, new CreatePlayerJsonPromptBuilder(storageService, loggingService) },
                { PromptType.Summarize, new SummarizePromptBuilder(storageService, loggingService) }
            };
        }

        public async Task<Prompt> BuildPromptAsync(PromptRequest request)
        {
            try
            {
                // Check if we have a builder for this prompt type
                if (_promptBuilders.TryGetValue(request.PromptType, out var builder))
                {
                    return await builder.BuildPromptAsync(request);
                }
                
                // If we get here, we don't have a builder for the requested prompt type
                throw new ArgumentException($"Unsupported prompt type: {request.PromptType}");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error building prompt: {ex.Message}");
                throw;
            }
        }
    }
}
