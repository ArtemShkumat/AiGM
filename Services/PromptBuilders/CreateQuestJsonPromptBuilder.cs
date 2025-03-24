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

        public override async Task<Prompt> BuildPromptAsync(string userId, string userInput)
        {
            try
            {
                // Get world and player info from userId
                var world = await _storageService.GetWorldAsync(userId);
                var player = await _storageService.GetPlayerAsync(userId);
                
                // System prompt
                var systemPrompt = await _storageService.GetCreateQuestJsonTemplateAsync("SystemCreateQuestJson");
                
                // Response instructions and example responses
                var responseInstructions = await _storageService.GetCreateQuestJsonTemplateAsync("ResponseInstructions");
                var exampleResponses = await _storageService.GetCreateQuestJsonTemplateAsync("ExampleResponses");

                // Create the system prompt builder
                var systemPromptBuilder = new StringBuilder();
                systemPromptBuilder.AppendLine(systemPrompt);
                
                // Add response instructions and examples to system prompt
                new TemplatePromptSection("Response Instructions", responseInstructions).AppendTo(systemPromptBuilder);
                new TemplatePromptSection("Example Responses", exampleResponses).AppendTo(systemPromptBuilder);

                // Create the prompt content builder
                var promptContentBuilder = new StringBuilder();

                // Add world context
                new WorldLoreSummarySection(world).AppendTo(promptContentBuilder);

                // Add player context
                new PlayerContextSection(player).AppendTo(promptContentBuilder);

                // Add the user's input containing the quest description
                new UserInputSection(userInput, "Quest Description to Convert to JSON").AppendTo(promptContentBuilder);

                return new Prompt(
                    systemPrompt: systemPromptBuilder.ToString(),
                    promptContent: promptContentBuilder.ToString(),
                    promptType: PromptType.CreateQuestJson
                );
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error building create quest JSON prompt: {ex.Message}");
                throw;
            }
        }
    }
} 