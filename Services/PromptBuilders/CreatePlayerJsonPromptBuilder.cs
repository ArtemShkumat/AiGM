using AiGMBackEnd.Models;
using AiGMBackEnd.Models.Prompts.Sections;
using System.Text;

namespace AiGMBackEnd.Services.PromptBuilders
{
    public class CreatePlayerJsonPromptBuilder : BasePromptBuilder
    {
        public CreatePlayerJsonPromptBuilder(StorageService storageService, LoggingService loggingService)
            : base(storageService, loggingService)
        {
        }

        public override async Task<string> BuildPromptAsync(string userId, string userInput)
        {
            try
            {
                // Load create player JSON template files
                var systemPrompt = await _storageService.GetCreatePlayerJsonTemplateAsync("SystemCreatePlayerJson");
                var exampleResponses = await _storageService.GetCreatePlayerJsonTemplateAsync("ExampleResponses");

                // Load world data for context
                var world = await _storageService.GetWorldAsync(userId);
                var gameSetting = await _storageService.GetGameSettingAsync(userId);
                var gamePreferences = await _storageService.GetGamePreferencesAsync(userId);

                // Create the final prompt
                var promptBuilder = new StringBuilder();
                promptBuilder.AppendLine(systemPrompt);
                promptBuilder.AppendLine();

                // Add example responses
                new TemplatePromptSection("Example Prompts and Responses", exampleResponses).AppendTo(promptBuilder);

                // Add game setting and preferences using section helpers
                new GameSettingSection(gameSetting).AppendTo(promptBuilder);
                new GamePreferencesSection(gamePreferences).AppendTo(promptBuilder);

                // Add world context
                new WorldLoreSummarySection(world).AppendTo(promptBuilder);

                // Add player ID info
                promptBuilder.AppendLine("# Player Info");
                promptBuilder.AppendLine($"Player ID: {userId}");
                promptBuilder.AppendLine();

                // Add the user's input containing the player description
                new UserInputSection(userInput, "Player Description to Convert to JSON").AppendTo(promptBuilder);

                return promptBuilder.ToString();
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error building player JSON prompt: {ex.Message}");
                throw;
            }
        }
    }
} 