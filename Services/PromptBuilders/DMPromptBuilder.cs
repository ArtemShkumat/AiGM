using AiGMBackEnd.Models;
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

        public override async Task<Prompt> BuildPromptAsync(string userId, string userInput)
        {
            try
            {
                await _storageService.AddUserMessageAsync(userId, userInput);                

                // Load DM template files
                var systemPrompt = await _storageService.GetDmTemplateAsync("System");
                var outputStructure = await _storageService.GetDmTemplateAsync("OutputStructure");
                var exampleResponses = await _storageService.GetDmTemplateAsync("ExampleResponses");

                // Load player and world data
                var player = await _storageService.GetPlayerAsync(userId);
                var world = await _storageService.GetWorldAsync(userId);
                var gameSetting = await _storageService.GetGameSettingAsync(userId);
                var gamePreferences = await _storageService.GetGamePreferencesAsync(userId);
                var location = await _storageService.GetLocationAsync(userId, player.CurrentLocationId);
                var parentLocation = await _storageService.GetLocationAsync(userId, location.ParentLocation);
                var connectedLocations = new List<Location>();
                foreach (var cl in location.ConnectedLocations)
                {
                    connectedLocations.Add(await _storageService.GetLocationAsync(userId, cl));
                }
                var npcsInCurrentLocation = await _storageService.GetNpcsInLocationAsync(userId, player.CurrentLocationId);
                var activeQuests = await _storageService.GetActiveQuestsAsync(userId, player.ActiveQuests);
                var conversationLog = await _storageService.GetConversationLogAsync(userId);

                var promptContentBuilder = new StringBuilder();

                new GameSettingSection(gameSetting).AppendTo(promptContentBuilder);
                new GamePreferencesSection(gamePreferences).AppendTo(promptContentBuilder);
                new WorldContextSection(world, includeEntityLists: true).AppendTo(promptContentBuilder);
                new WorldLoreSummarySection(world).AppendTo(promptContentBuilder);
                new PlayerContextSection(player).AppendTo(promptContentBuilder);

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
                    promptContentBuilder.AppendLine("# NPCs Present");
                    foreach (var npc in npcsInCurrentLocation)
                    {
                        new NPCSection(npc).AppendTo(promptContentBuilder);
                    }
                }

                // Add all active quests and all their information
                if (activeQuests != null && activeQuests.Count > 0)
                {
                    promptContentBuilder.AppendLine("# Active Quests");
                    foreach (var quest in activeQuests)
                    {
                        new QuestSection(quest).AppendTo(promptContentBuilder);
                    }
                }

                // Add conversation history
                new ConversationLogSection(conversationLog).AppendTo(promptContentBuilder);
                
                // Add the user's input
                //new UserInputSection(userInput, "Current player prompt").AppendTo(promptContentBuilder);

                // Build the system prompt with response instructions and examples
                var systemPromptBuilder = new StringBuilder();
                systemPromptBuilder.AppendLine(systemPrompt);
                systemPromptBuilder.AppendLine();
                
                // Add output structure
                PromptSection.AppendSection(systemPromptBuilder, "Output Structure", outputStructure);

                // Add example responses
                systemPromptBuilder.AppendLine("# Here are some examples of prompts and responses for you to follow:");
                PromptSection.AppendSection(systemPromptBuilder, "Example Responses", exampleResponses);
                
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