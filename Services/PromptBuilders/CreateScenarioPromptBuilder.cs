using AiGMBackEnd.Models;
using AiGMBackEnd.Models.Prompts;
using AiGMBackEnd.Models.Prompts.Sections;
using System.Text;
using System;
using System.Threading.Tasks;

namespace AiGMBackEnd.Services.PromptBuilders
{
    public class CreateScenarioPromptBuilder : BasePromptBuilder
    {
        public CreateScenarioPromptBuilder(StorageService storageService, LoggingService loggingService)
            : base(storageService, loggingService)
        {
        }

        public override async Task<Prompt> BuildPromptAsync(PromptRequest request)
        {
            try
            {
                // Load create scenario template files
                var systemPrompt = await _storageService.GetCreateScenarioTemplateAsync("System.txt");
                var outputStructure = await _storageService.GetCreateScenarioTemplateAsync("OutputStructure.json");
                var exampleResponses = await _storageService.GetCreateScenarioTemplateAsync("ExampleResponses.txt");

                // Create the system prompt builder
                var systemPromptBuilder = new StringBuilder();
                systemPromptBuilder.AppendLine(systemPrompt);
                systemPromptBuilder.AppendLine();
                
                // Add examples to system prompt
                new TemplatePromptSection("Example Responses", exampleResponses).AppendTo(systemPromptBuilder);

                // Create the prompt content builder
                var promptContentBuilder = new StringBuilder();

                // Add the user's scenario prompt
                new UserInputSection(request.UserInput, "Scenario Request").AppendTo(promptContentBuilder);

                return new Prompt(
                    systemPrompt: systemPromptBuilder.ToString(),
                    promptContent: promptContentBuilder.ToString(),
                    promptType: PromptType.CreateScenario,
                    outputStructureJsonSchema: outputStructure
                );
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error building create scenario prompt: {ex.Message}");
                throw;
            }
        }
    }
} 