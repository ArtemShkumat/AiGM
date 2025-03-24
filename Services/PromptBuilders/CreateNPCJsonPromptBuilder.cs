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

        public override async Task<Prompt> BuildPromptAsync(string userId, string userInput)
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

                // Create the system prompt builder
                var systemPromptBuilder = new StringBuilder();
                systemPromptBuilder.AppendLine(systemPrompt);
                systemPromptBuilder.AppendLine();
                
                // Add response instructions and examples to system prompt
                new TemplatePromptSection("Response Instructions", responseInstructions).AppendTo(systemPromptBuilder);
                new TemplatePromptSection("Example Responses", exampleResponses).AppendTo(systemPromptBuilder);

                // Create the prompt content builder
                var promptContentBuilder = new StringBuilder();

                // Add world context
                new WorldLoreSummarySection(world).AppendTo(promptContentBuilder);

                // Add player context
                new PlayerContextSection(player).AppendTo(promptContentBuilder);

                // Add the user's input containing the NPC description
                new UserInputSection(userInput, "NPC Description to Convert to JSON").AppendTo(promptContentBuilder);

                return new Prompt(
                    systemPrompt: systemPromptBuilder.ToString(),
                    promptContent: promptContentBuilder.ToString(),
                    promptType: PromptType.CreateNPCJson
                );
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error building NPC JSON prompt: {ex.Message}");
                throw;
            }
        }
    }
} 