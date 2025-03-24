using AiGMBackEnd.Models;
using AiGMBackEnd.Models.Prompts.Sections;
using System.Text;

namespace AiGMBackEnd.Services.PromptBuilders
{
    public class CreateLocationJsonPromptBuilder : BasePromptBuilder
    {
        public CreateLocationJsonPromptBuilder(StorageService storageService, LoggingService loggingService)
            : base(storageService, loggingService)
        {
        }

        public override async Task<Prompt> BuildPromptAsync(string userId, string userInput)
        {
            try
            {
                // Load create location JSON template files
                var systemPrompt = await _storageService.GetCreateLocationJsonTemplateAsync("SystemCreateLocationJson");
                var responseInstructions = await _storageService.GetCreateLocationJsonTemplateAsync("ResponseInstructions");
                var exampleResponses = await _storageService.GetCreateLocationJsonTemplateAsync("ExampleResponses");

                // Load world data for context
                var world = await _storageService.GetWorldAsync(userId);
                var gameSetting = await _storageService.GetGameSettingAsync(userId);
                var gamePreferences = await _storageService.GetGamePreferencesAsync(userId);
                var player = await _storageService.GetPlayerAsync(userId);

                // Create the system prompt builder
                var systemPromptBuilder = new StringBuilder();
                systemPromptBuilder.AppendLine(systemPrompt);
                systemPromptBuilder.AppendLine();
                
                // Add response instructions and examples to system prompt
                new TemplatePromptSection("Response Instructions", responseInstructions).AppendTo(systemPromptBuilder);
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

                // Add trigger instructions
                new TriggerInstructionsSection("This location is being created based on a specific need in the game world.").AppendTo(promptContentBuilder);

                // Add the user's input
                new UserInputSection(userInput, "Location Description to Convert to JSON").AppendTo(promptContentBuilder);

                return new Prompt(
                    systemPrompt: systemPromptBuilder.ToString(),
                    promptContent: promptContentBuilder.ToString(),
                    promptType: PromptType.CreateLocationJson
                );
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error building location JSON prompt: {ex.Message}");
                throw;
            }
        }
    }
} 