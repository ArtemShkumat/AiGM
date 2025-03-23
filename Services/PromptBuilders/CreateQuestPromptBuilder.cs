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

        public override async Task<string> BuildPromptAsync(string userId, string userInput)
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

                // Add player RPG elements if available
                if (player.RpgElements != null && player.RpgElements.ContainsKey("class"))
                {
                    promptBuilder.AppendLine($"Class: {player.RpgElements["class"]}");
                }
                
                if (player.RpgElements != null && player.RpgElements.ContainsKey("level"))
                {
                    promptBuilder.AppendLine($"Level: {player.RpgElements["level"]}");
                }
                
                if (!string.IsNullOrEmpty(player.Backstory))
                {
                    promptBuilder.AppendLine($"Background: {player.Backstory}");
                }
                promptBuilder.AppendLine();

                // Add trigger instructions
                new TriggerInstructionsSection("This quest is being created based on a specific trigger in the game world.").AppendTo(promptBuilder);

                // Add response instructions
                PromptSection.AppendSection(promptBuilder, "Response Instructions", responseInstructions);

                // Add example responses
                PromptSection.AppendSection(promptBuilder, "Example Responses", exampleResponses);

                // Add the user's input
                new UserInputSection(userInput, "Quest Request").AppendTo(promptBuilder);

                return promptBuilder.ToString();
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error building create quest prompt: {ex.Message}");
                throw;
            }
        }
    }
} 