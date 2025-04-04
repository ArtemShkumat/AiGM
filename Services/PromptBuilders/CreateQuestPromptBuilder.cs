using AiGMBackEnd.Models;
using AiGMBackEnd.Models.Prompts;
using AiGMBackEnd.Models.Prompts.Sections;
using System.Text;
using System;

namespace AiGMBackEnd.Services.PromptBuilders
{
    public class CreateQuestPromptBuilder : BasePromptBuilder
    {
        public CreateQuestPromptBuilder(StorageService storageService, LoggingService loggingService)
            : base(storageService, loggingService)
        {
        }

        public override async Task<Prompt> BuildPromptAsync(PromptRequest request)
        {
            try
            {
                // Load create quest template files
                var systemPrompt = await _storageService.GetCreateQuestTemplateAsync("System");
                var outputStructure = await _storageService.GetCreateQuestTemplateAsync("OutputStructure");
                var exampleResponses = await _storageService.GetCreateQuestTemplateAsync("ExampleResponses");

                // Load player and world data for context
                var player = await _storageService.GetPlayerAsync(request.UserId);
                var world = await _storageService.GetWorldAsync(request.UserId);
                var gameSetting = await _storageService.GetGameSettingAsync(request.UserId);
                var gamePreferences = await _storageService.GetGamePreferencesAsync(request.UserId);

                // Create the system prompt builder
                var systemPromptBuilder = new StringBuilder();
                systemPromptBuilder.AppendLine(systemPrompt);
                systemPromptBuilder.AppendLine();
                
                // Add output structure and examples to system prompt
                PromptSection.AppendSection(systemPromptBuilder, "Output Structure", outputStructure);
                PromptSection.AppendSection(systemPromptBuilder, "Example Responses", exampleResponses);

                // Create the prompt content builder
                var promptContentBuilder = new StringBuilder();

                // Add game setting and preferences using section helpers
                new GameSettingSection(gameSetting).AppendTo(promptContentBuilder);
                new GamePreferencesSection(gamePreferences).AppendTo(promptContentBuilder);
                new WorldContextSection(world).AppendTo(promptContentBuilder);
                new WorldLoreSummarySection(world).AppendTo(promptContentBuilder);
                new PlayerContextSection(player).AppendTo(promptContentBuilder);
                
                promptContentBuilder.AppendLine();

                // Add NPC creation details
                new CreateQuestSection(
                    request.QuestId,
                    request.QuestName,
                    request.Context
                ).AppendTo(promptContentBuilder);

                return new Prompt(
                    systemPrompt: systemPromptBuilder.ToString(),
                    promptContent: promptContentBuilder.ToString(),
                    promptType: PromptType.CreateQuest
                );
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error building create quest prompt: {ex.Message}");
                throw;
            }
        }
    }
} 