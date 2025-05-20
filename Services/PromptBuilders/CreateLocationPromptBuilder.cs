using AiGMBackEnd.Models;
using AiGMBackEnd.Models.Locations;
using AiGMBackEnd.Models.Prompts;
using AiGMBackEnd.Models.Prompts.Sections;
using System;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Linq;

namespace AiGMBackEnd.Services.PromptBuilders
{
    public class CreateLocationPromptBuilder : BasePromptBuilder
    {
        public CreateLocationPromptBuilder(
            StorageService storageService, 
            LoggingService loggingService) 
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
            // Use "Generic" if the type doesn't have a specific folder (e.g., Region, Landmark)
            string locationTypeSubDir = request.LocationType;
            string templateBasePath = $"Create/Location/{locationTypeSubDir}/"; 

            // Check if a specific template directory exists, otherwise fall back to Generic
            bool useGenericTemplate = !new[] { "Building", "Settlement", "Delve", "Wilds" }.Contains(request.LocationType, StringComparer.OrdinalIgnoreCase);
            if (useGenericTemplate)
            {
                locationTypeSubDir = "Generic";
                templateBasePath = $"Create/Location/{locationTypeSubDir}/";
                _loggingService.LogInfo($"Using Generic location templates for type: {request.LocationType}");
            }
            else
            {
                _loggingService.LogInfo($"Using specific location templates for type: {request.LocationType}");
            }

            try
            {
                // Load create location template files
                var systemPrompt = await _storageService.GetTemplateAsync(templateBasePath + "System.txt");
                var outputStructure = await _storageService.GetTemplateAsync(templateBasePath + "OutputStructure.json");
                var exampleResponses = await _storageService.GetTemplateAsync(templateBasePath + "ExampleResponses.txt");

                // Extract scenario ID from request or metadata if provided
                string scenarioId = request.ScenarioId;
                
                // No need to check metadata as we're now using ScenarioId directly

                // Load context data based on the available identifiers
                World world = null;
                GameSetting gameSetting = null; 
                GamePreferences gamePreferences = null;
                Location parentLocation = null;

                _loggingService.LogInfo($"Loading data for location creation - UserId: {request.UserId}, ScenarioId: {scenarioId}");

                // Load world and game settings (from scenario if available, otherwise from user data)
                world = await _storageService.GetWorldAsync(request.UserId, scenarioId);
                gameSetting = await _storageService.GetGameSettingAsync(request.UserId, scenarioId);
                
                // Only attempt to load game preferences from user data (not scenarios)
                if (string.IsNullOrEmpty(scenarioId))
                {
                    gamePreferences = await _storageService.GetGamePreferencesAsync(request.UserId);
                }
                
                // Check for parent location if ID provided
                if (!string.IsNullOrEmpty(request.ParentLocationId))
                {
                    parentLocation = await _storageService.GetLocationAsync(request.UserId, request.ParentLocationId, scenarioId);
                    if (parentLocation != null)
                    {
                        _loggingService.LogInfo($"Found parent location: {parentLocation.Name} (ID: {parentLocation.Id})");
                    }
                }

                // Create the system prompt builder
                var systemPromptBuilder = new StringBuilder();
                systemPromptBuilder.AppendLine(systemPrompt);
                systemPromptBuilder.AppendLine();

                // Add examples to system prompt
                new TemplatePromptSection("Example Responses", exampleResponses).AppendTo(systemPromptBuilder);

                // Create the prompt content builder
                var promptContentBuilder = new StringBuilder();

                // Add game setting and preferences
                if (gameSetting != null)
                    new GameSettingSection(gameSetting, false).AppendTo(promptContentBuilder);
                
                if (gamePreferences != null)
                    new GamePreferencesSection(gamePreferences).AppendTo(promptContentBuilder);

                // Add world context
                if (world != null)
                    new WorldContextSection(world).AppendTo(promptContentBuilder);

                // Add parent location context if available
                if (parentLocation != null)
                {
                    new LocationContextSection(parentLocation, "parentLocationContext").AppendTo(promptContentBuilder);
                }

                // Add location creation details
                new CreateLocationSection(
                    request.LocationId,
                    request.LocationName,
                    request.LocationType,
                    request.Context,
                    request.ParentLocationId
                ).AppendTo(promptContentBuilder);

                return new Prompt(
                    systemPrompt: systemPromptBuilder.ToString(),
                    promptContent: promptContentBuilder.ToString(),
                    promptType: PromptType.CreateLocation,
                    outputStructureJsonSchema: outputStructure
                );
            }
            catch (FileNotFoundException fnfEx)
            {
                _loggingService.LogError($"Template file not found for LocationType '{request.LocationType}' (BasePath: {templateBasePath}): {fnfEx.Message}");
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