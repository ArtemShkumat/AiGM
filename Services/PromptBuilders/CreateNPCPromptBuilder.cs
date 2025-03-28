using AiGMBackEnd.Models;
using AiGMBackEnd.Models.Prompts;
using AiGMBackEnd.Models.Prompts.Sections;
using System.Text;
using System;

namespace AiGMBackEnd.Services.PromptBuilders
{
    public class CreateNPCPromptBuilder : BasePromptBuilder
    {
        public CreateNPCPromptBuilder(StorageService storageService, LoggingService loggingService)
            : base(storageService, loggingService)
        {
        }

        public override async Task<Prompt> BuildPromptAsync(PromptRequest request)
        {
            try
            {
                // Load create NPC template files
                var systemPrompt = await _storageService.GetCreateNpcTemplateAsync("System");
                var outputStructure = await _storageService.GetCreateNpcTemplateAsync("OutputStructure");
                var exampleResponses = await _storageService.GetCreateNpcTemplateAsync("ExampleResponses");

                // Load world data for context
                var world = await _storageService.GetWorldAsync(request.UserId);
                var gameSetting = await _storageService.GetGameSettingAsync(request.UserId);
                var gamePreferences = await _storageService.GetGamePreferencesAsync(request.UserId);
                //var player = await _storageService.GetPlayerAsync(request.UserId);
                var location = await _storageService.GetLocationAsync(request.UserId, request.NpcLocation);

                // Create the system prompt builder
                var systemPromptBuilder = new StringBuilder();
                systemPromptBuilder.AppendLine(systemPrompt);
                systemPromptBuilder.AppendLine();
                
                // Add output structure and examples to system prompt
                new TemplatePromptSection("Output Structure", outputStructure).AppendTo(systemPromptBuilder);
                new TemplatePromptSection("Example Responses", exampleResponses).AppendTo(systemPromptBuilder);

                // Create the prompt content builder
                var promptContentBuilder = new StringBuilder();

                // Add game setting and preferences using section helpers
                new GameSettingSection(gameSetting).AppendTo(promptContentBuilder);
                new GamePreferencesSection(gamePreferences).AppendTo(promptContentBuilder);

                // Add world context
                new WorldContextSection(world).AppendTo(promptContentBuilder);
                
                // Add player context
                //new PlayerContextSection(player).AppendTo(promptContentBuilder);

                // Add location context
                if (location != null)
                {
                    new LocationContextSection(location).AppendTo(promptContentBuilder);
                }

                // Add NPC creation details
                new CreateNpcSection(
                    request.NpcName,
                    request.NpcId,
                    request.NpcLocation,
                    request.Context
                ).AppendTo(promptContentBuilder);

                //

                // Add the user's input
                //new UserInputSection(request.UserInput, "NPC Creation Request").AppendTo(promptContentBuilder);

                return new Prompt(
                    systemPrompt: systemPromptBuilder.ToString(),
                    promptContent: promptContentBuilder.ToString(),
                    promptType: PromptType.CreateNPC
                );
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error building create NPC prompt: {ex.Message}");
                throw;
            }
        }
    }
} 