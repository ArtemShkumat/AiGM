using AiGMBackEnd.Models;
using AiGMBackEnd.Models.Prompts;
using AiGMBackEnd.Models.Prompts.Sections;
using System.Text;
using System;
using System.Threading.Tasks;
using System.IO; // For Path.Combine

namespace AiGMBackEnd.Services.PromptBuilders
{
    public class CreateLocationPromptBuilder : BasePromptBuilder
    {
        public CreateLocationPromptBuilder(StorageService storageService, LoggingService loggingService)
            : base(storageService, loggingService)
        {
        }

        public override async Task<Prompt> BuildPromptAsync(PromptRequest request)
        {
            if (string.IsNullOrEmpty(request.LocationType))
            {
                throw new ArgumentException("LocationType must be specified in the PromptRequest for location creation.");
            }

            // Determine the subdirectory based on LocationType
            string locationTypeSubDir = request.LocationType; // Assuming LocationType matches directory names (Building, Delve, etc.)
            string templateBasePath = $"Create/Location/{locationTypeSubDir}/"; // Path relative to the root template dir

            try
            {
                // Load create location template files from the specific type's directory
                var systemPrompt = await _storageService.GetTemplateAsync(templateBasePath + "System.txt");
                var outputStructure = await _storageService.GetTemplateAsync(templateBasePath + "OutputStructure.json");
                var exampleResponses = await _storageService.GetTemplateAsync(templateBasePath + "ExampleResponses.txt");

                // Load context data
                var world = await _storageService.GetWorldAsync(request.UserId);
                var gameSetting = await _storageService.GetGameSettingAsync(request.UserId);
                var gamePreferences = await _storageService.GetGamePreferencesAsync(request.UserId);
                // Optional: Load parent location if ID provided?

                // Create the system prompt builder
                var systemPromptBuilder = new StringBuilder();
                systemPromptBuilder.AppendLine(systemPrompt);
                systemPromptBuilder.AppendLine();

                // Add examples to system prompt
                new TemplatePromptSection("Example Responses", exampleResponses).AppendTo(systemPromptBuilder);

                // Create the prompt content builder
                var promptContentBuilder = new StringBuilder();

                // Add game setting and preferences
                new GameSettingSection(gameSetting, false).AppendTo(promptContentBuilder);
                new GamePreferencesSection(gamePreferences).AppendTo(promptContentBuilder);

                // Add world context
                new WorldContextSection(world).AppendTo(promptContentBuilder);

                // Add location creation details
                new CreateLocationSection(
                    request.LocationId,
                    request.LocationName,
                    request.LocationType,
                    request.Context
                ).AppendTo(promptContentBuilder);

                return new Prompt(
                    systemPrompt: systemPromptBuilder.ToString(),
                    promptContent: promptContentBuilder.ToString(),
                    promptType: PromptType.CreateLocation,
                    outputStructureJsonSchema: outputStructure // Pass the specific schema
                );
            }
            catch (FileNotFoundException fnfEx)
            {
                _loggingService.LogError($"Template file not found for LocationType '{request.LocationType}': {fnfEx.Message}");
                throw new InvalidOperationException($"Could not load required templates for location type '{request.LocationType}'. Ensure templates exist in PromptTemplates/{templateBasePath}", fnfEx);
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error building create location prompt ({request.LocationType}): {ex.Message}");
                throw;
            }
        }
    }
} 