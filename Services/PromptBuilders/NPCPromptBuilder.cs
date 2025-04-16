using AiGMBackEnd.Models;
using AiGMBackEnd.Models.Prompts;
using AiGMBackEnd.Models.Prompts.Sections;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Collections.Generic;

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
                await _storageService.AddUserMessageToNpcLogAsync(request.UserId, request.NpcId, request.UserInput);

                // Load NPC template files
                var systemPrompt = await _storageService.GetNpcTemplateAsync("System.txt");
                // Use DM OutputStructure.json to ensure consistent format
                var outputStructure = await _storageService.GetDmTemplateAsync("OutputStructure.json");
                var exampleResponses = await _storageService.GetNpcTemplateAsync("ExampleResponses.txt");

                // Load player, world, and specified NPC data
                var player = await _storageService.GetPlayerAsync(request.UserId);
                var world = await _storageService.GetWorldAsync(request.UserId);
                var npc = await _storageService.GetNpcAsync(request.UserId, request.NpcId);
                var gameSetting = await _storageService.GetGameSettingAsync(request.UserId);
                var gamePreferences = await _storageService.GetGamePreferencesAsync(request.UserId);
                var location = await _storageService.GetLocationAsync(request.UserId, player.CurrentLocationId);   
                //var conversationLog = await _storageService.GetConversationLogAsync(request.UserId);
                var userInput = request.UserInput;
               
                // Create system prompt builder
                var systemPromptBuilder = new StringBuilder();
                systemPromptBuilder.AppendLine(systemPrompt);
                systemPromptBuilder.AppendLine();
                
                // Add example responses to system prompt
                PromptSection.AppendSection(systemPromptBuilder, "Example Responses", exampleResponses);

                // Create dictionary for prompt content (main content first)
                var promptContentDict = new Dictionary<string, object>
                {
                    ["gameSetting"] = gameSetting,
                    ["gamePreferences"] = gamePreferences,
                    ["worldContext"] = world,
                    ["playerContext"] = player,
                    ["currentLocationDetails"] = location,
                    ["npcContext"] = npc
                };

                // Add the items in the specific order required (conversationLog, playerInput)
                //if (conversationLog != null)
                //{
                //    promptContentDict["conversationLog"] = conversationLog;
                //}

                // Always add playerInput last
                promptContentDict["playerInput"] = userInput;

                // Use a custom JsonSerializerOptions to preserve property order
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = null
                };
                
                // Create a JsonObject to maintain property order in serialization
                var jsonObject = new JsonObject();
                foreach (var kvp in promptContentDict)
                {
                    jsonObject.Add(kvp.Key, JsonSerializer.SerializeToNode(kvp.Value, options));
                }
                
                string serializedPromptContent = jsonObject.ToJsonString(options);

                return new Prompt(
                    systemPrompt: systemPromptBuilder.ToString(),
                    promptContent: serializedPromptContent,
                    promptType: PromptType.NPC,
                    outputStructureJsonSchema: outputStructure
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