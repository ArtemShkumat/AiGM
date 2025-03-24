using AiGMBackEnd.Models;
using AiGMBackEnd.Models.Prompts.Sections;
using System.Text;

namespace AiGMBackEnd.Services.PromptBuilders
{
    public class CreateQuestPromptBuilder : BasePromptBuilder
    {
        public CreateQuestPromptBuilder(StorageService storageService, LoggingService loggingService)
            : base(storageService, loggingService)
        {
        }

        public override async Task<Prompt> BuildPromptAsync(string userId, string userInput)
        {
            try
            {
                // Load create quest template files
                var systemPrompt = await _storageService.GetCreateQuestTemplateAsync("SystemCreateQuest");
                var responseInstructions = await _storageService.GetCreateQuestTemplateAsync("ResponseInstructions");
                var exampleResponses = await _storageService.GetCreateQuestTemplateAsync("ExampleResponses");

                // Load player and world data for context
                var player = await _storageService.GetPlayerAsync(userId);
                var world = await _storageService.GetWorldAsync(userId);
                var gameSetting = await _storageService.GetGameSettingAsync(userId);
                var gamePreferences = await _storageService.GetGamePreferencesAsync(userId);

                // Create the system prompt builder
                var systemPromptBuilder = new StringBuilder();
                systemPromptBuilder.AppendLine(systemPrompt);
                systemPromptBuilder.AppendLine();
                
                // Add response instructions and examples to system prompt
                PromptSection.AppendSection(systemPromptBuilder, "Response Instructions", responseInstructions);
                PromptSection.AppendSection(systemPromptBuilder, "Example Responses", exampleResponses);

                // Create the prompt content builder
                var promptContentBuilder = new StringBuilder();

                // Add game setting and preferences using section helpers
                new GameSettingSection(gameSetting).AppendTo(promptContentBuilder);
                new GamePreferencesSection(gamePreferences).AppendTo(promptContentBuilder);

                // Add world context
                new WorldLoreSummarySection(world).AppendTo(promptContentBuilder);

                // Add player context
                new PlayerContextSection(player).AppendTo(promptContentBuilder);

                // Add player RPG elements if available
                if (player.RpgElements != null && player.RpgElements.ContainsKey("class"))
                {
                    promptContentBuilder.AppendLine($"Class: {player.RpgElements["class"]}");
                }
                
                if (player.RpgElements != null && player.RpgElements.ContainsKey("level"))
                {
                    promptContentBuilder.AppendLine($"Level: {player.RpgElements["level"]}");
                }
                
                if (!string.IsNullOrEmpty(player.Backstory))
                {
                    promptContentBuilder.AppendLine($"Background: {player.Backstory}");
                }
                promptContentBuilder.AppendLine();

                // Add trigger instructions
                new TriggerInstructionsSection("This quest is being created based on a specific trigger in the game world.").AppendTo(promptContentBuilder);

                // Add the user's input
                new UserInputSection(userInput, "Quest Request").AppendTo(promptContentBuilder);

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