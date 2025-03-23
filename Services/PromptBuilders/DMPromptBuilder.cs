using AiGMBackEnd.Models;
using AiGMBackEnd.Models.Prompts.Sections;
using System.Text;

namespace AiGMBackEnd.Services.PromptBuilders
{
    public class DMPromptBuilder : BasePromptBuilder
    {
        public DMPromptBuilder(StorageService storageService, LoggingService loggingService)
            : base(storageService, loggingService)
        {
        }

        public override async Task<string> BuildPromptAsync(string userId, string userInput)
        {
            try
            {
                // Load DM template files
                var systemPrompt = await _storageService.GetDmTemplateAsync("SystemDM");
                var responseInstructions = await _storageService.GetDmTemplateAsync("ResponseInstructions");
                var exampleResponses = await _storageService.GetDmTemplateAsync("ExampleResponses");

                // Load player and world data
                var player = await _storageService.GetPlayerAsync(userId);
                var world = await _storageService.GetWorldAsync(userId);
                var gameSetting = await _storageService.GetGameSettingAsync(userId);
                var gamePreferences = await _storageService.GetGamePreferencesAsync(userId);
                var location = await _storageService.GetLocationAsync(userId, player.CurrentLocationId);
                var npcsInCurrentLocation = await _storageService.GetNpcsInLocationAsync(userId, player.CurrentLocationId);
                var activeQuests = await _storageService.GetActiveQuestsAsync(userId, player.ActiveQuests);
                var conversationLog = await _storageService.GetConversationLogAsync(userId);

                // Create the final prompt
                var promptBuilder = new StringBuilder();
                promptBuilder.AppendLine(systemPrompt);
                promptBuilder.AppendLine();

                // Add response instructions
                PromptSection.AppendSection(promptBuilder, "Response Instructions", responseInstructions);

                // Add example responses
                promptBuilder.AppendLine("# Here are some examples of prompts and responses for you to follow:");
                PromptSection.AppendSection(promptBuilder, "Example Responses", exampleResponses);

                promptBuilder.AppendLine("# Below is the current game state data");

                // Add sections using our helper classes
                new GameSettingSection(gameSetting).AppendTo(promptBuilder);
                new GamePreferencesSection(gamePreferences).AppendTo(promptBuilder);
                
                // Add world context section
                new WorldContextSection(world, includeEntityLists: true).AppendTo(promptBuilder);

                // Add additional world lore summary if needed
                new WorldLoreSummarySection(world).AppendTo(promptBuilder);
                
                // Add player characters context
                new PlayerContextSection(player).AppendTo(promptBuilder);

                // Add location context using the location section
                new LocationContextSection(location).AppendTo(promptBuilder);

                // Add NPCs present at this location and all the information about them
                if (npcsInCurrentLocation != null && npcsInCurrentLocation.Count > 0)
                {
                    promptBuilder.AppendLine("# NPCs Present");
                    foreach (var npc in npcsInCurrentLocation)
                    {
                        new NPCSection(npc).AppendTo(promptBuilder);
                    }
                }
                else
                {
                    promptBuilder.AppendLine("# NPCs Present");
                    promptBuilder.AppendLine("There are no NPCs currently present at this location.");
                    promptBuilder.AppendLine();
                }

                // Add all active quests and all their information
                if (activeQuests != null && activeQuests.Count > 0)
                {
                    promptBuilder.AppendLine("# Active Quests");
                    foreach (var quest in activeQuests)
                    {
                        new QuestSection(quest).AppendTo(promptBuilder);
                    }
                }
                else
                {
                    promptBuilder.AppendLine("# Active Quests");
                    promptBuilder.AppendLine("The player currently has no active quests.");
                    promptBuilder.AppendLine();
                }

                // Add conversation history
                new ConversationLogSection(conversationLog).AppendTo(promptBuilder);
                
                // Add the user's input
                new UserInputSection(userInput, "Current player prompt").AppendTo(promptBuilder);

                return promptBuilder.ToString();
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error building DM prompt: {ex.Message}");
                throw;
            }
        }
    }
} 