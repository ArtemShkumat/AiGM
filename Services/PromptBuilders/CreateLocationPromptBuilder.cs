using AiGMBackEnd.Models;
using AiGMBackEnd.Models.Prompts.Sections;
using System.Text;

namespace AiGMBackEnd.Services.PromptBuilders
{
    public class CreateLocationPromptBuilder : BasePromptBuilder
    {
        public CreateLocationPromptBuilder(StorageService storageService, LoggingService loggingService)
            : base(storageService, loggingService)
        {
        }

        public override async Task<string> BuildPromptAsync(string userId, string userInput)
        {
            try
            {
                // Load create location template files
                var systemPrompt = await _storageService.GetCreateLocationTemplateAsync("SystemCreateLocation");
                var responseInstructions = await _storageService.GetCreateLocationTemplateAsync("ResponseInstructions");
                var exampleResponses = await _storageService.GetCreateLocationTemplateAsync("ExampleResponses");

                // Load world data for context
                var world = await _storageService.GetWorldAsync(userId);
                var gameSetting = await _storageService.GetGameSettingAsync(userId);
                var gamePreferences = await _storageService.GetGamePreferencesAsync(userId);
                var player = await _storageService.GetPlayerAsync(userId);

                // Create the final prompt
                var promptBuilder = new StringBuilder();
                promptBuilder.AppendLine(systemPrompt);
                promptBuilder.AppendLine();

                // Add game setting and preferences using section helpers
                new GameSettingSection(gameSetting).AppendTo(promptBuilder);
                new GamePreferencesSection(gamePreferences).AppendTo(promptBuilder);

                // Add world context
                new WorldLoreSummarySection(world).AppendTo(promptBuilder);
                
                // Add player context
                new PlayerContextSection(player).AppendTo(promptBuilder);

                // Add trigger instructions
                new TriggerInstructionsSection("This location is being created based on a specific need in the game world.").AppendTo(promptBuilder);

                // Add response instructions and examples using section helpers
                new TemplatePromptSection("Response Instructions", responseInstructions).AppendTo(promptBuilder);
                new TemplatePromptSection("Example Responses", exampleResponses).AppendTo(promptBuilder);

                // Add the user's input
                new UserInputSection(userInput, "Location Creation Request").AppendTo(promptBuilder);

                return promptBuilder.ToString();
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error building create location prompt: {ex.Message}");
                throw;
            }
        }
    }
} 