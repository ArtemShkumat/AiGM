using AiGMBackEnd.Models;
using AiGMBackEnd.Models.Prompts.Sections;
using System.Text;

namespace AiGMBackEnd.Services.PromptBuilders
{
    public class NPCPromptBuilder : BasePromptBuilder
    {
        public NPCPromptBuilder(StorageService storageService, LoggingService loggingService)
            : base(storageService, loggingService)
        {
        }

        public override async Task<string> BuildPromptAsync(string userId, string userInput)
        {
            return await BuildPromptAsync(userId, userInput, null);
        }

        public async Task<string> BuildPromptAsync(string userId, string userInput, string providedNpcId)
        {
            try
            {
                // Use provided NpcId if available, otherwise parse from userInput
                string npcId = !string.IsNullOrEmpty(providedNpcId) 
                    ? providedNpcId 
                    : ParseNpcId(userInput);
                
                // Load NPC template files
                var systemPrompt = await _storageService.GetNpcTemplateAsync("SystemNPC");
                var responseInstructions = await _storageService.GetNpcTemplateAsync("ResponseInstructions");
                var exampleResponses = await _storageService.GetNpcTemplateAsync("ExampleResponses");

                // Load player, world, and specified NPC data
                var player = await _storageService.GetPlayerAsync(userId);
                var world = await _storageService.GetWorldAsync(userId);
                var npc = await _storageService.GetNpcAsync(userId, npcId);
                var gameSetting = await _storageService.GetGameSettingAsync(userId);
                var gamePreferences = await _storageService.GetGamePreferencesAsync(userId);

                // Load current location
                var location = await _storageService.GetLocationAsync(userId, player.CurrentLocationId);

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

                // Create the final prompt
                var promptBuilder = new StringBuilder();
                promptBuilder.AppendLine(systemPrompt);
                promptBuilder.AppendLine();

                // Add game setting and preferences using our helper classes
                new GameSettingSection(gameSetting).AppendTo(promptBuilder);
                new GamePreferencesSection(gamePreferences).AppendTo(promptBuilder);

                // Add NPC context
                promptBuilder.AppendLine("# NPC Context");
                promptBuilder.AppendLine($"NPC Name: {npc.Name}");
                if (npc.VisualDescription != null)
                {
                    promptBuilder.AppendLine($"Age: {npc.VisualDescription.Body}");
                }
                if (npc.KnownEntities != null)
                {
                    promptBuilder.AppendLine($"Role: {npc.Personality.Quirks}");
                }
                promptBuilder.AppendLine($"Description: {npc.Backstory}");
                promptBuilder.AppendLine($"Personality: {npc.Personality.Temperament}");
                promptBuilder.AppendLine($"Knowledge: {(npc.KnownEntities != null ? "Has knowledge of various entities" : "Limited knowledge")}");
                promptBuilder.AppendLine($"Goals: {npc.DispositionTowardsPlayer}");
                promptBuilder.AppendLine();

                // Add scene context
                promptBuilder.AppendLine("# Scene Context");
                promptBuilder.AppendLine($"Location: {location.Name}");
                promptBuilder.AppendLine($"Location Description: {location.Description}");
                promptBuilder.AppendLine($"Time: {world.GameTime}");                
                promptBuilder.AppendLine();

                // Add player context using simplified player context section
                new PlayerContextSection(player).AppendTo(promptBuilder);

                // Add NPC's relationship with player if it exists
                if (npc.KnowsPlayer)
                {
                    promptBuilder.AppendLine($"Disposition towards player: {npc.DispositionTowardsPlayer}");
                }
                promptBuilder.AppendLine();

                // Add conversation history if available
                if (npc.ConversationLog != null && npc.ConversationLog.Count > 0)
                {
                    promptBuilder.AppendLine("# Previous Conversation");
                    foreach (var entry in npc.ConversationLog)
                    {
                        foreach (var item in entry)
                        {
                            promptBuilder.AppendLine($"{item.Key}: {item.Value}");
                        }
                    }
                    promptBuilder.AppendLine();
                }

                // Add response instructions
                PromptSection.AppendSection(promptBuilder, "Response Instructions", responseInstructions);

                // Add example responses
                PromptSection.AppendSection(promptBuilder, "Example Responses", exampleResponses);

                // Add the user's input
                new UserInputSection(userInput, "User Input").AppendTo(promptBuilder);

                return promptBuilder.ToString();
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error building NPC prompt: {ex.Message}");
                throw;
            }
        }
        
        private string ParseNpcId(string userInput)
        {
            // Basic parsing to extract NPC ID from user input
            // Format expected: "talk to <npc_name>" or "interact with <npc_id>"
            // This is a simple implementation - could be enhanced with regex or more sophisticated parsing
            
            if (userInput.Contains("npc_id:"))
            {
                var parts = userInput.Split("npc_id:");
                if (parts.Length > 1)
                {
                    var idPart = parts[1].Trim();
                    return idPart.Split(' ')[0]; // Take first part before any space
                }
            }
            
            // Default to a placeholder - in a real implementation, you'd want better NPC targeting logic
            _loggingService.LogWarning($"Could not parse NPC ID from input: {userInput}. Using default logic.");
            return "generic_npc";
        }
    }
} 