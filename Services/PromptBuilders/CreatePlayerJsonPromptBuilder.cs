using AiGMBackEnd.Models;
using AiGMBackEnd.Models.Prompts;
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

        public override async Task<Prompt> BuildPromptAsync(PromptRequest request)
        {
            try
            {
                // Load create player JSON template files
                var systemPrompt = await _storageService.GetCreatePlayerJsonTemplateAsync("System");
                var exampleResponses = await _storageService.GetCreatePlayerJsonTemplateAsync("ExampleResponses");

                // Load world data for context
                var world = await _storageService.GetWorldAsync(request.UserId);
                var gameSetting = await _storageService.GetGameSettingAsync(request.UserId);
                var gamePreferences = await _storageService.GetGamePreferencesAsync(request.UserId);

                // Create the system prompt builder
                var systemPromptBuilder = new StringBuilder();
                systemPromptBuilder.AppendLine(systemPrompt);
                systemPromptBuilder.AppendLine();

                // Add example responses to system prompt
                new TemplatePromptSection("Example Prompts and Responses", exampleResponses).AppendTo(systemPromptBuilder);

                // Create the prompt content builder
                var promptContentBuilder = new StringBuilder();

                // Add game setting and preferences using section helpers
                new GameSettingSection(gameSetting).AppendTo(promptContentBuilder);
                new GamePreferencesSection(gamePreferences).AppendTo(promptContentBuilder);

                // Add world context
                new WorldLoreSummarySection(world).AppendTo(promptContentBuilder);

                // Add player ID info
                promptContentBuilder.AppendLine("# Player Info");
                promptContentBuilder.AppendLine($"Player ID: {request.UserId}");
                promptContentBuilder.AppendLine();

                // Add the user's input containing the player description
                new UserInputSection(request.UserInput, "Player Description to Convert to JSON").AppendTo(promptContentBuilder);

                return new Prompt(
                    systemPrompt: systemPromptBuilder.ToString(),
                    promptContent: promptContentBuilder.ToString(),
                    promptType: PromptType.CreatePlayerJson
                );
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error building player JSON prompt: {ex.Message}");
                throw;
            }
        }
    }
}