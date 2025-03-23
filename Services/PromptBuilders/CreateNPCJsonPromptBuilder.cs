using AiGMBackEnd.Models;
using AiGMBackEnd.Models.Prompts.Sections;
using System.Text;

namespace AiGMBackEnd.Services.PromptBuilders
{
    public class CreateNPCJsonPromptBuilder : BasePromptBuilder
    {
        public CreateNPCJsonPromptBuilder(StorageService storageService, LoggingService loggingService)
            : base(storageService, loggingService)
        {
        }

        public override async Task<string> BuildPromptAsync(string userId, string userInput)
        {
            try
            {
                // Get world and player info
                var world = await _storageService.GetWorldAsync(userId);
                var player = await _storageService.GetPlayerAsync(userId);

                // Load create NPC JSON template files
                var systemPrompt = await _storageService.GetCreateNpcJsonTemplateAsync("SystemCreateNPCJson");
                var responseInstructions = await _storageService.GetCreateNpcJsonTemplateAsync("ResponseInstructions");
                var exampleResponses = await _storageService.GetCreateNpcJsonTemplateAsync("ExampleResponses");

                // Create the final prompt
                var promptBuilder = new StringBuilder();
                promptBuilder.AppendLine(systemPrompt);
                promptBuilder.AppendLine();

                // Add world context
                new WorldLoreSummarySection(world).AppendTo(promptBuilder);

                // Add player context
                new PlayerContextSection(player).AppendTo(promptBuilder);

                // Add response instructions and examples using section helpers
                new TemplatePromptSection("Response Instructions", responseInstructions).AppendTo(promptBuilder);
                new TemplatePromptSection("Example Responses", exampleResponses).AppendTo(promptBuilder);

                // Add the user's input containing the NPC description
                new UserInputSection(userInput, "NPC Description to Convert to JSON").AppendTo(promptBuilder);

                return promptBuilder.ToString();
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error building NPC JSON prompt: {ex.Message}");
                throw;
            }
        }
    }
} 