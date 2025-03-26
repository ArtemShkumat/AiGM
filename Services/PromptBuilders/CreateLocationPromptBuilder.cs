using AiGMBackEnd.Models;
using AiGMBackEnd.Models.Prompts.Sections;
using System.Text;
using System;

namespace AiGMBackEnd.Services.PromptBuilders
{
    public class CreateLocationPromptBuilder : BasePromptBuilder
    {
        public CreateLocationPromptBuilder(StorageService storageService, LoggingService loggingService)
            : base(storageService, loggingService)
        {
        }

        public override async Task<Prompt> BuildPromptAsync(string userId, string userInput)
        {
            // Default to generic location (without specific type)
            return await BuildPromptAsync(userId, userInput, null);
        }

        public override async Task<Prompt> BuildPromptAsync(string userId, string userInput, string locationType = null)
        {
            try
            {
                _loggingService.LogInfo($"Building location prompt for type: {locationType ?? "generic"}");
                
                // Load create location template files
                var systemPrompt = await _storageService.GetCreateLocationTemplateAsync("System", locationType);
                var outputStructure = await _storageService.GetCreateLocationTemplateAsync("OutputStructure", locationType);
                var exampleResponses = await _storageService.GetCreateLocationTemplateAsync("Examples", locationType);

                // Load world data for context
                var world = await _storageService.GetWorldAsync(userId);
                var gameSetting = await _storageService.GetGameSettingAsync(userId);
                var gamePreferences = await _storageService.GetGamePreferencesAsync(userId);
                var player = await _storageService.GetPlayerAsync(userId);

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
                new WorldLoreSummarySection(world).AppendTo(promptContentBuilder);
                
                // Add player context
                new PlayerContextSection(player).AppendTo(promptContentBuilder);

                // Add the user's input
                new UserInputSection(userInput, $"{(locationType ?? "Location")} Creation Request").AppendTo(promptContentBuilder);

                var promptType = PromptType.CreateLocation;
                
                return new Prompt(
                    systemPrompt: systemPromptBuilder.ToString(),
                    promptContent: promptContentBuilder.ToString(),
                    promptType: promptType
                );
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error building create location prompt: {ex.Message}");
                throw;
            }
        }
    }
} 