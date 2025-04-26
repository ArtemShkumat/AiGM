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
        private readonly Random _random = new Random();
        
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
                    // Get parent location if it exists
                    if (!string.IsNullOrEmpty(location.ParentLocation))
                    {
                        var parentLocation = await _storageService.GetLocationAsync(request.UserId, location.ParentLocation);
                        if (parentLocation != null)
                        {
                            promptContentDict["parentLocation"] = parentLocation;
                        }
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
                
                // Check for random event
                string randomEventDirective = AddRandomEvent(world.GameTime, world, request.UserId);
                if (!string.IsNullOrEmpty(randomEventDirective))
                {
                    systemPromptBuilder.AppendLine(randomEventDirective);
                    systemPromptBuilder.AppendLine();
                }
                
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
        
        /// <summary>
        /// Determines if a random event should occur and returns the event directive if it should.
        /// </summary>
        /// <param name="currentGameTime">The current game time</param>
        /// <param name="world">The world object</param>
        /// <param name="userId">The user ID for saving updated world state</param>
        /// <returns>The random event directive text if an event should occur, otherwise null</returns>
        private string AddRandomEvent(DateTimeOffset currentGameTime, World world, string userId)
        {
            try
            {
                // Default values - 10% chance every 24 hours
                const int chancePercent = 10;
                const int cooldownHours = 24;
                
                // Check if cooldown has passed
                bool cooldownPassed = world.LastRandomEventTime == null || 
                                      (currentGameTime - world.LastRandomEventTime.Value).TotalHours >= cooldownHours;
                
                if (cooldownPassed)
                {
                    // Generate random number between 1-100
                    int roll = _random.Next(1, 101);
                    
                    // If the roll is less than or equal to the chance percentage, trigger an event
                    if (roll <= chancePercent)
                    {
                        // Update LastRandomEventTime and save the world
                        world.LastRandomEventTime = currentGameTime;
                        _storageService.SaveAsync(userId, "world", world).Wait();
                        
                        // Get the random event directive template
                        return _storageService.GetDmTemplateAsync("random_event_directive.txt").Result;
                    }
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error in AddRandomEvent: {ex.Message}");
                return null; // On error, don't add an event
            }
        }
    }
} 