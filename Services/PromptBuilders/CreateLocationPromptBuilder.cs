using AiGMBackEnd.Models;
using AiGMBackEnd.Models.Prompts;
using AiGMBackEnd.Models.Prompts.Sections;
using System.Text;
using System;

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
            try
            {
                _loggingService.LogInfo($"Building location prompt for type: {request.LocationType ?? "Building"}");
                
                // Load create location template files
                var systemPrompt = await _storageService.GetCreateLocationTemplateAsync("System", request.LocationType);
                var outputStructure = await _storageService.GetCreateLocationTemplateAsync("OutputStructure", request.LocationType);
                var exampleResponses = await _storageService.GetCreateLocationTemplateAsync("ExampleResponses", request.LocationType);

                // Load world data for context
                var world = await _storageService.GetWorldAsync(request.UserId);
                var gameSetting = await _storageService.GetGameSettingAsync(request.UserId);
                var gamePreferences = await _storageService.GetGamePreferencesAsync(request.UserId);
                var player = await _storageService.GetPlayerAsync(request.UserId);

                // Create the system prompt builder
                var systemPromptBuilder = new StringBuilder();
                systemPromptBuilder.AppendLine(systemPrompt);
                systemPromptBuilder.AppendLine();
                
                // Add output structure and examples to system prompt
                new TemplatePromptSection("Output Structure", outputStructure).AppendTo(systemPromptBuilder);
                new TemplatePromptSection("Example Responses", exampleResponses).AppendTo(systemPromptBuilder);

                // Create the prompt content builder
                var promptContentBuilder = new StringBuilder();

                // Add game setting and preferences using section helpers
                new GameSettingSection(gameSetting, false).AppendTo(promptContentBuilder);
                new GamePreferencesSection(gamePreferences).AppendTo(promptContentBuilder);
                new WorldContextSection(world).AppendTo(promptContentBuilder);

                // Add NPC creation details
                new CreateLocationSection(
                    request.LocationType,
                    request.LocationId,
                    request.LocationName,
                    request.Context
                ).AppendTo(promptContentBuilder);

                //// Add world context
                //new WorldLoreSummarySection(world).AppendTo(promptContentBuilder);

                // Add player context
                //new PlayerContextSection(player).AppendTo(promptContentBuilder);

                // Add the user's input
                //new UserInputSection(request.UserInput, $"{(request.LocationType ?? "Location")} Creation Request").AppendTo(promptContentBuilder);

                var promptType = PromptType.CreateLocation;
                
                return new Prompt(
                    systemPrompt: systemPromptBuilder.ToString(),
                    promptContent: promptContentBuilder.ToString(),
                    promptType: promptType
                );
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error building create location prompt: {ex.Message}");
                throw;
            }
        }
    }
} 