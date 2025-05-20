using AiGMBackEnd.Models;
using AiGMBackEnd.Models.Prompts;
using AiGMBackEnd.Models.Prompts.Sections;
using AiGMBackEnd.Services.Storage;
using System;
using System.Text;
using System.Threading.Tasks;

namespace AiGMBackEnd.Services.PromptBuilders
{
    public class GenerateScenarioTemplatePromptBuilder : IPromptBuilder
    {
        private readonly ITemplateService _templateService;
        private readonly LoggingService _loggingService;

        public GenerateScenarioTemplatePromptBuilder(ITemplateService templateService, LoggingService loggingService)
        {
            _templateService = templateService;
            _loggingService = loggingService;
        }

        public async Task<Prompt> BuildPromptAsync(PromptRequest request)
        {
            try
            {
                // Validate request type
                if (request.PromptType != PromptType.GenerateScenarioTemplate)
                {
                    throw new ArgumentException($"Invalid prompt type for GenerateScenarioTemplatePromptBuilder: {request.PromptType}");
                }

                // Validate context (the large text input)
                if (string.IsNullOrEmpty(request.Context))
                {
                    throw new ArgumentException("Request context (large text input) cannot be null or empty");
                }

                // Load template files
                string systemPrompt = await _templateService.GetTemplateAsync("System/GenerateScenarioTemplate/system.txt");
                string outputStructure = await _templateService.GetTemplateAsync("System/GenerateScenarioTemplate/outputStructure.json");
                
                // Note: We'll use the outputStructure JSON as the schema for the LLM
                // We don't need to pass the exampleResponse separately since it's included in the system prompt

                // Build system prompt
                var systemPromptBuilder = new StringBuilder();
                systemPromptBuilder.AppendLine(systemPrompt);
                systemPromptBuilder.AppendLine();
                
                // Add example to system prompt
                string exampleResponse = await _templateService.GetTemplateAsync("System/GenerateScenarioTemplate/exampleResponse.json");
                systemPromptBuilder.AppendLine("# Example Response");
                systemPromptBuilder.AppendLine("Here is an example of a well-formatted response:");
                systemPromptBuilder.AppendLine("```json");
                systemPromptBuilder.AppendLine(exampleResponse);
                systemPromptBuilder.AppendLine("```");
                
                // Build content
                var promptContentBuilder = new StringBuilder();
                
                // Add the large text input as content
                promptContentBuilder.AppendLine("# Source Text for Scenario Generation");
                promptContentBuilder.AppendLine();
                promptContentBuilder.AppendLine(request.Context);

                return new Prompt(
                    systemPrompt: systemPromptBuilder.ToString(),
                    promptContent: promptContentBuilder.ToString(),
                    promptType: PromptType.GenerateScenarioTemplate,
                    outputStructureJsonSchema: outputStructure
                );
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error building generate scenario template prompt: {ex.Message}");
                throw;
            }
        }
    }
} 