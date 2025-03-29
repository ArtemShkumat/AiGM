using AiGMBackEnd.Models;
using AiGMBackEnd.Models.Prompts;
using AiGMBackEnd.Models.Prompts.Sections;
using System.Text;
using System;

namespace AiGMBackEnd.Services.PromptBuilders
{
    public class NPCPromptBuilder : BasePromptBuilder
    {
        public NPCPromptBuilder(StorageService storageService, LoggingService loggingService)
            : base(storageService, loggingService)
        {
        }

        public override async Task<Prompt> BuildPromptAsync(PromptRequest request)
        {
            try
            {
                await _storageService.AddUserMessageToNpcLogAsync(request.UserId, request.NpcId, request.UserInput);

                // Load NPC template files
                var systemPrompt = await _storageService.GetNpcTemplateAsync("System");
                var outputStructure = await _storageService.GetNpcTemplateAsync("OutputStructure");
                var exampleResponses = await _storageService.GetNpcTemplateAsync("ExampleResponses");

                // Load player, world, and specified NPC data
                var player = await _storageService.GetPlayerAsync(request.UserId);
                var world = await _storageService.GetWorldAsync(request.UserId);
                var npc = await _storageService.GetNpcAsync(request.UserId, request.NpcId);
                var gameSetting = await _storageService.GetGameSettingAsync(request.UserId);
                var gamePreferences = await _storageService.GetGamePreferencesAsync(request.UserId);
                var location = await _storageService.GetLocationAsync(request.UserId, player.CurrentLocationId);               
               
                // Create system prompt builder
                var systemPromptBuilder = new StringBuilder();
                systemPromptBuilder.AppendLine(systemPrompt);
                systemPromptBuilder.AppendLine();
                
                // Add output structure to system prompt
                PromptSection.AppendSection(systemPromptBuilder, "Detailed information on fields that can be used for partial updates", outputStructure);

                // Add example responses to system prompt
                PromptSection.AppendSection(systemPromptBuilder, "Example Responses", exampleResponses);

                // Create prompt content builder
                var promptContentBuilder = new StringBuilder();

                // Add game setting and preferences using our helper classes
                new GameSettingSection(gameSetting).AppendTo(promptContentBuilder);
                new GamePreferencesSection(gamePreferences).AppendTo(promptContentBuilder);
                new WorldContextSection(world, includeEntityLists: true).AppendTo(promptContentBuilder);
                promptContentBuilder.Append("currentLocation:");
                new LocationContextSection(location).AppendTo(promptContentBuilder);
                new PlayerContextSection(player, false).AppendTo(promptContentBuilder);

                // Add NPC context
                promptContentBuilder.AppendLine("#This is context about your character:");
                new NPCSection(npc, true).AppendTo(promptContentBuilder);

                // Add the user's input
                new UserInputSection(request.UserInput, "User Input").AppendTo(promptContentBuilder);

                return new Prompt(
                    systemPrompt: systemPromptBuilder.ToString(),
                    promptContent: promptContentBuilder.ToString(),
                    promptType: PromptType.NPC
                );
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error building NPC prompt: {ex.Message}");
                throw;
            }
        }

        private string ParseNpcId(string userInput)
        {
            // Implementation of NPC ID parsing logic
            // This should be implemented based on your specific requirements
            throw new NotImplementedException("NPC ID parsing logic needs to be implemented");
        }
    }
} 