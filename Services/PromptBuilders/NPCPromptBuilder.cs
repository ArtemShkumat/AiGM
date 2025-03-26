using AiGMBackEnd.Models;
using AiGMBackEnd.Models.Prompts;
using AiGMBackEnd.Models.Prompts.Sections;
using System.Text;
using System;

namespace AiGMBackEnd.Services.PromptBuilders
{
    public class NPCPromptBuilder : BasePromptBuilder
    {
        public NPCPromptBuilder(StorageService storageService, LoggingService loggingService)
            : base(storageService, loggingService)
        {
        }

        public override async Task<Prompt> BuildPromptAsync(PromptRequest request)
        {
            try
            {
                // Use provided NpcId if available, otherwise parse from userInput
                string npcId = !string.IsNullOrEmpty(request.NpcId) 
                    ? request.NpcId 
                    : ParseNpcId(request.UserInput);
                
                // Load NPC template files
                var systemPrompt = await _storageService.GetNpcTemplateAsync("System");
                var outputStructure = await _storageService.GetNpcTemplateAsync("OutputStructure");
                var exampleResponses = await _storageService.GetNpcTemplateAsync("ExampleResponses");

                // Load player, world, and specified NPC data
                var player = await _storageService.GetPlayerAsync(request.UserId);
                var world = await _storageService.GetWorldAsync(request.UserId);
                var npc = await _storageService.GetNpcAsync(request.UserId, npcId);
                var gameSetting = await _storageService.GetGameSettingAsync(request.UserId);
                var gamePreferences = await _storageService.GetGamePreferencesAsync(request.UserId);

                // Load current location
                var location = await _storageService.GetLocationAsync(request.UserId, player.CurrentLocationId);

                // Create scene context
                var sceneContext = new SceneContext
                {
                    GameTime = world.GameTime,
                    LocationInfo = new SceneLocationInfo 
                    { 
                        Name = location.Name,
                        Description = location.Description
                    }
                };

                // Create system prompt builder
                var systemPromptBuilder = new StringBuilder();
                systemPromptBuilder.AppendLine(systemPrompt);
                systemPromptBuilder.AppendLine();
                
                // Add output structure to system prompt
                PromptSection.AppendSection(systemPromptBuilder, "Output Structure", outputStructure);

                // Add example responses to system prompt
                PromptSection.AppendSection(systemPromptBuilder, "Example Responses", exampleResponses);

                // Create prompt content builder
                var promptContentBuilder = new StringBuilder();

                // Add game setting and preferences using our helper classes
                new GameSettingSection(gameSetting).AppendTo(promptContentBuilder);
                new GamePreferencesSection(gamePreferences).AppendTo(promptContentBuilder);

                // Add NPC context
                promptContentBuilder.AppendLine("# NPC Context");
                promptContentBuilder.AppendLine($"NPC Name: {npc.Name}");
                if (npc.VisualDescription != null)
                {
                    promptContentBuilder.AppendLine($"Age: {npc.VisualDescription.Body}");
                }
                if (npc.KnownEntities != null)
                {
                    promptContentBuilder.AppendLine($"Role: {npc.Personality.Quirks}");
                }
                promptContentBuilder.AppendLine($"Description: {npc.Backstory}");
                promptContentBuilder.AppendLine($"Personality: {npc.Personality.Temperament}");
                promptContentBuilder.AppendLine($"Knowledge: {(npc.KnownEntities != null ? "Has knowledge of various entities" : "Limited knowledge")}");
                promptContentBuilder.AppendLine($"Goals: {npc.DispositionTowardsPlayer}");
                promptContentBuilder.AppendLine();

                // Add scene context
                promptContentBuilder.AppendLine("# Scene Context");
                promptContentBuilder.AppendLine($"Location: {location.Name}");
                promptContentBuilder.AppendLine($"Location Description: {location.Description}");
                promptContentBuilder.AppendLine($"Time: {world.GameTime}");                
                promptContentBuilder.AppendLine();

                // Add player context using simplified player context section
                new PlayerContextSection(player).AppendTo(promptContentBuilder);

                // Add NPC's relationship with player if it exists
                if (npc.KnowsPlayer)
                {
                    promptContentBuilder.AppendLine($"Disposition towards player: {npc.DispositionTowardsPlayer}");
                }
                promptContentBuilder.AppendLine();

                // Add conversation history if available
                if (npc.ConversationLog != null && npc.ConversationLog.Count > 0)
                {
                    promptContentBuilder.AppendLine("# Previous Conversation");
                    foreach (var entry in npc.ConversationLog)
                    {
                        foreach (var item in entry)
                        {
                            promptContentBuilder.AppendLine($"{item.Key}: {item.Value}");
                        }
                    }
                    promptContentBuilder.AppendLine();
                }

                // Add the user's input
                new UserInputSection(request.UserInput, "User Input").AppendTo(promptContentBuilder);

                return new Prompt(
                    systemPrompt: systemPromptBuilder.ToString(),
                    promptContent: promptContentBuilder.ToString(),
                    promptType: PromptType.NPC
                );
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error building NPC prompt: {ex.Message}");
                throw;
            }
        }

        private string ParseNpcId(string userInput)
        {
            // Implementation of NPC ID parsing logic
            // This should be implemented based on your specific requirements
            throw new NotImplementedException("NPC ID parsing logic needs to be implemented");
        }
    }
} 