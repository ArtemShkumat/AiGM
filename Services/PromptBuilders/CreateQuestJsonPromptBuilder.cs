using AiGMBackEnd.Models;
using AiGMBackEnd.Models.Prompts.Sections;
using System.Text;
using Microsoft.Extensions.Logging;

namespace AiGMBackEnd.Services.PromptBuilders
{
    public class CreateQuestJsonPromptBuilder : BasePromptBuilder
    {
        public CreateQuestJsonPromptBuilder(StorageService storageService, LoggingService loggingService)
            : base(storageService, loggingService)
        {
        }

        public override async Task<string> BuildPromptAsync(string userId, string userInput)
        {
            try
            {
                // Get world and player info from userId
                var world = await _storageService.GetWorldAsync(userId);
                var player = await _storageService.GetPlayerAsync(userId);
                
                var promptBuilder = new StringBuilder();

                // System prompt
                var systemPrompt = await _storageService.GetCreateQuestJsonTemplateAsync("SystemCreateQuestJson");
                promptBuilder.AppendLine(systemPrompt);

                // Add world context
                new WorldLoreSummarySection(world).AppendTo(promptBuilder);

                // Add player context
                new PlayerContextSection(player).AppendTo(promptBuilder);

                // Add response instructions and example responses using section helpers
                var responseInstructions = await _storageService.GetCreateQuestJsonTemplateAsync("ResponseInstructions");
                var exampleResponses = await _storageService.GetCreateQuestJsonTemplateAsync("ExampleResponses");
                
                new TemplatePromptSection("Response Instructions", responseInstructions).AppendTo(promptBuilder);
                new TemplatePromptSection("Example Responses", exampleResponses).AppendTo(promptBuilder);

                // Add the user's input containing the quest description
                new UserInputSection(userInput, "Quest Description to Convert to JSON").AppendTo(promptBuilder);

                return promptBuilder.ToString();
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error building create quest JSON prompt: {ex.Message}");
                throw;
            }
        }
    }
} 