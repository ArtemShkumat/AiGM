using AiGMBackEnd.Models;
using AiGMBackEnd.Models.Prompts;
using AiGMBackEnd.Models.Prompts.Sections;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AiGMBackEnd.Models.Locations;
using System.Collections.Generic;

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
                await _storageService.AddUserMessageAsync(request.UserId, request.UserInput);
                // Load DM template files
                var systemPrompt = await _storageService.GetDmTemplateAsync("System.txt");
                var outputStructure = await _storageService.GetDmTemplateAsync("OutputStructure.json");
                var exampleResponses = await _storageService.GetDmTemplateAsync("ExampleResponses.txt");

                // Load player and world data
                var player = await _storageService.GetPlayerAsync(request.UserId);
                var world = await _storageService.GetWorldAsync(request.UserId);
                var gameSetting = await _storageService.GetGameSettingAsync(request.UserId);
                var gamePreferences = await _storageService.GetGamePreferencesAsync(request.UserId);
                var location = await _storageService.GetLocationAsync(request.UserId, player.CurrentLocationId);

                // Create ordered dictionary for prompt content
                var recentEvents = await _storageService.GetRecentEventsAsync(request.UserId);
                var conversationLog = await _storageService.GetConversationLogAsync(request.UserId);
                var userInput = request.UserInput;

                // Create dictionary for prompt content (main content first)
                var promptContentDict = new Dictionary<string, object>
                {
                    ["gameSetting"] = gameSetting,
                    ["gamePreferences"] = gamePreferences,
                    ["worldContext"] = world,
                    ["playerContext"] = player,
                    ["currentLocationDetails"] = location
                };

                // Process connected locations
                var connectedLocations = new List<Location>();
                if (location != null)
                {
                    foreach (var cl in location.ConnectedLocations)
                    {
                        var connectedLocation = await _storageService.GetLocationAsync(request.UserId, cl);
                        if (connectedLocation != null)
                        {
                            connectedLocations.Add(connectedLocation);
                        }
                    }
                    
                    // Get parent location if it exists
                    if (!string.IsNullOrEmpty(location.ParentLocation))
                    {
                        var parentLocation = await _storageService.GetLocationAsync(request.UserId, location.ParentLocation);
                        if (parentLocation != null)
                        {
                            promptContentDict["parentLocation"] = parentLocation;
                        }
                    }
                    
                    if (connectedLocations.Count > 0)
                    {
                        promptContentDict["connectedLocations"] = connectedLocations;
                    }
                }

                // Get NPCs in current location
                var npcsInCurrentLocation = await _storageService.GetNpcsInLocationAsync(request.UserId, player.CurrentLocationId);
                if (npcsInCurrentLocation != null && npcsInCurrentLocation.Count > 0)
                {
                    promptContentDict["npcsPresentInThisLocation"] = npcsInCurrentLocation;
                }

                // Get active quests
                var activeQuests = await _storageService.GetActiveQuestsAsync(request.UserId, player.ActiveQuests);
                if (activeQuests != null && activeQuests.Count > 0)
                {
                    promptContentDict["activeQuests"] = activeQuests;
                }

                // Add the items in the specific order required (recentEvents, conversationLog, playerInput)
                if (recentEvents != null)
                {
                    promptContentDict["recentEvents"] = recentEvents;
                }

                if (conversationLog != null)
                {
                    promptContentDict["conversationLog"] = conversationLog;
                }

                // Always add playerInput last
                promptContentDict["playerInput"] = userInput;

                // Build the system prompt with response instructions and examples
                var systemPromptBuilder = new StringBuilder();
                systemPromptBuilder.AppendLine(systemPrompt);
                systemPromptBuilder.AppendLine();
                
                // Add example responses
                systemPromptBuilder.AppendLine("# Here are some examples of prompts and responses for you to follow:");
                PromptSection.AppendSection(systemPromptBuilder, "Example Responses", exampleResponses);

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

                // Create the prompt object
                return new Prompt(
                    systemPrompt: systemPromptBuilder.ToString(),
                    promptContent: serializedPromptContent,
                    promptType: PromptType.DM,
                    outputStructureJsonSchema: outputStructure
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