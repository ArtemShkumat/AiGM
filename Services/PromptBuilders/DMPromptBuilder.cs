using AiGMBackEnd.Models;
using AiGMBackEnd.Models.Prompts;
using AiGMBackEnd.Models.Prompts.Sections;
using System.Text;
using System;
using AiGMBackEnd.Models.Locations;

namespace AiGMBackEnd.Services.PromptBuilders
{
    public class DMPromptBuilder : BasePromptBuilder
    {
        public DMPromptBuilder(StorageService storageService, LoggingService loggingService)
            : base(storageService, loggingService)
        {
        }

        public override async Task<Prompt> BuildPromptAsync(PromptRequest request)
        {
            try
            {
                         

                // Load DM template files
                var systemPrompt = await _storageService.GetDmTemplateAsync("System");
                var outputStructure = await _storageService.GetDmTemplateAsync("OutputStructure");
                var exampleResponses = await _storageService.GetDmTemplateAsync("ExampleResponses");

                // Load player and world data
                var player = await _storageService.GetPlayerAsync(request.UserId);
                var world = await _storageService.GetWorldAsync(request.UserId);
                var gameSetting = await _storageService.GetGameSettingAsync(request.UserId);
                var gamePreferences = await _storageService.GetGamePreferencesAsync(request.UserId);
                var location = await _storageService.GetLocationAsync(request.UserId, player.CurrentLocationId);
                var parentLocation = await _storageService.GetLocationAsync(request.UserId, location.ParentLocation);
                var connectedLocations = new List<Location>();
                foreach (var cl in location.ConnectedLocations)
                {
                    connectedLocations.Add(await _storageService.GetLocationAsync(request.UserId, cl));
                }
                var npcsInCurrentLocation = await _storageService.GetNpcsInLocationAsync(request.UserId, player.CurrentLocationId);
                var activeQuests = await _storageService.GetActiveQuestsAsync(request.UserId, player.ActiveQuests);
                var conversationLog = await _storageService.GetConversationLogAsync(request.UserId);

                var promptContentBuilder = new StringBuilder();

                new GameSettingSection(gameSetting).AppendTo(promptContentBuilder);
                new GamePreferencesSection(gamePreferences).AppendTo(promptContentBuilder);
                new WorldContextSection(world, includeEntityLists: true).AppendTo(promptContentBuilder);
                new WorldLoreSummarySection(world).AppendTo(promptContentBuilder);
                new PlayerContextSection(player).AppendTo(promptContentBuilder);

                // Build the system prompt with response instructions and examples
                var systemPromptBuilder = new StringBuilder();
                systemPromptBuilder.AppendLine(systemPrompt);
                systemPromptBuilder.AppendLine();

                // Add output structure
                PromptSection.AppendSection(systemPromptBuilder, "Output Structure", outputStructure);

                // Add example responses
                systemPromptBuilder.AppendLine("# Here are some examples of prompts and responses for you to follow:");
                PromptSection.AppendSection(systemPromptBuilder, "Example Responses", exampleResponses);

                // Add location context using the location section
                promptContentBuilder.AppendLine("currentLocation: ");
                new LocationContextSection(location).AppendTo(promptContentBuilder);
                if (!string.IsNullOrEmpty(location.ParentLocation))
                {
                    promptContentBuilder.AppendLine("parentLocation: ");
                    new LocationContextSection(parentLocation).AppendTo(promptContentBuilder);
                }

                if (connectedLocations!=null && connectedLocations.Count>0)
                {
                    promptContentBuilder.AppendLine("connectedLocations: ");
                    foreach (var item in connectedLocations)
                    {
                        new LocationContextSection(item).AppendTo(promptContentBuilder);
                    }
                }                

                // Add NPCs present at this location and all the information about them
                if (npcsInCurrentLocation != null && npcsInCurrentLocation.Count > 0)
                {
                    promptContentBuilder.AppendLine("npcsPresentInThisLocation:");
                    foreach (var npc in npcsInCurrentLocation)
                    {
                        new NPCSection(npc, false).AppendTo(promptContentBuilder);
                    }
                }

                // Add all active quests and all their information
                if (activeQuests != null && activeQuests.Count > 0)
                {
                    promptContentBuilder.AppendLine("activeQuests:");
                    foreach (var quest in activeQuests)
                    {
                        new QuestSection(quest).AppendTo(promptContentBuilder);
                    }
                }

                // Add conversation history
                new ConversationLogSection(conversationLog).AppendTo(promptContentBuilder);
                
                // Add the user's input
                new UserInputSection(request.UserInput, "Current player prompt").AppendTo(promptContentBuilder);
                await _storageService.AddUserMessageAsync(request.UserId, request.UserInput);

                // Create the prompt object
                return new Prompt(
                    systemPrompt: systemPromptBuilder.ToString(),
                    promptContent: promptContentBuilder.ToString(),
                    promptType: PromptType.DM
                );
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error building DM prompt: {ex.Message}");
                throw;
            }
        }
    }
} 