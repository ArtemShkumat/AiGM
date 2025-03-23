using AiGMBackEnd.Models;
using System.Text;
using System;
using System.Linq;

namespace AiGMBackEnd.Services
{
    public enum PromptType
    {
        DM,
        NPC,
        CreateQuest,
        CreateQuestJson,
        CreateNPC,
        CreateNPCJson,
        CreateLocation,
        CreateLocationJson,
        CreatePlayerJson
    }

    public class PromptService
    {
        private readonly StorageService _storageService;
        private readonly LoggingService _loggingService;

        public PromptService(
            StorageService storageService,
            LoggingService loggingService)
        {
            _storageService = storageService;
            _loggingService = loggingService;
        }

        public async Task<string> BuildPromptAsync(PromptType promptType, string userId, string userInput)
        {
            try
            {
                switch (promptType)
                {
                    case PromptType.DM:
                        return await BuildDMPromptAsync(userId, userInput);
                    case PromptType.NPC:
                        string npcId = ParseNpcId(userInput);
                        return await BuildNPCPromptAsync(userId, npcId, userInput);
                    case PromptType.CreateQuest:
                        return await BuildCreateQuestPromptAsync(userId, userInput);
                    case PromptType.CreateQuestJson:
                        return await BuildCreateQuestJsonPromptAsync(userId, userInput);
                    case PromptType.CreateNPC:
                        return await BuildCreateNPCPromptAsync(userId, userInput);
                    case PromptType.CreateNPCJson:
                        return await BuildCreateNPCJsonPromptAsync(userId, userInput);
                    case PromptType.CreateLocation:
                        return await BuildCreateLocationPromptAsync(userId, userInput);
                    case PromptType.CreateLocationJson:
                        return await BuildCreateLocationJsonPromptAsync(userId, userInput);
                    case PromptType.CreatePlayerJson:
                        return await BuildCreatePlayerJsonPromptAsync(userId, userInput);
                    default:
                        throw new ArgumentException($"Unsupported prompt type: {promptType}");
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error building prompt: {ex.Message}");
                throw;
            }
        }

        private async Task<string> BuildDMPromptAsync(string userId, string userInput)
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
            promptBuilder.AppendLine("# Response Instructions");
            promptBuilder.AppendLine(responseInstructions);
            promptBuilder.AppendLine();

            // Add example responses
            promptBuilder.AppendLine("# Here are some examples of prompts and responses for you to follow:");
            promptBuilder.AppendLine("# Example Responses");
            promptBuilder.AppendLine(exampleResponses);
            promptBuilder.AppendLine();

            promptBuilder.AppendLine("# Below is the current game state data");

            // Add game setting and preferences
            promptBuilder.AppendLine("# Game Setting");
            promptBuilder.AppendLine($"Genre: {gameSetting.Genre}");
            promptBuilder.AppendLine($"Theme: {gameSetting.Theme}");
            promptBuilder.AppendLine($"Description: {gameSetting.Description}");
            promptBuilder.AppendLine($"Setting: {gameSetting.Setting}");
            promptBuilder.AppendLine();

            // Add game preferences
            promptBuilder.AppendLine("# Game Preferences");
            promptBuilder.AppendLine($"Tone: {gamePreferences.Tone}");
            promptBuilder.AppendLine($"Complexity: {gamePreferences.Complexity}");
            promptBuilder.AppendLine($"Age Appropriateness: {gamePreferences.AgeAppropriateness}");
            promptBuilder.AppendLine();
                       

            // Add world context
            promptBuilder.AppendLine("# World Context");
            promptBuilder.AppendLine($"Current Time: {world.GameTime}");
            promptBuilder.AppendLine($"Current Weather: {world.WorldStateEffects.Weather}");
            promptBuilder.AppendLine($"Days Since Start: {world.DaysSinceStart}");
                        
            // Add world lore summaries
            if (world.Lore != null && world.Lore.Count > 0)
            {
                promptBuilder.AppendLine("World Lore:");
                foreach (var lore in world.Lore)
                {
                    promptBuilder.AppendLine($"- {lore.Title}: {lore.Summary}");
                }
            }
            
            // Add all NPC names and IDs
            if (world.Npcs != null && world.Npcs.Count > 0)
            {
                promptBuilder.AppendLine("Existing NPCs:");
                foreach (var npc in world.Npcs)
                {
                    promptBuilder.AppendLine($"- {npc.Name} (ID: {npc.Id})");
                }
            }
            
            // Add all Location names and IDs
            if (world.Locations != null && world.Locations.Count > 0)
            {
                promptBuilder.AppendLine("Existing Locations:");
                foreach (var loc in world.Locations)
                {
                    promptBuilder.AppendLine($"- {loc.Name} (ID: {loc.Id})");
                }
            }

            //Add all existing quest names and IDs
            if (world.Quests != null && world.Quests.Count > 0)
            {
                promptBuilder.AppendLine("Existing Quests:");
                foreach (var q in world.Quests)
                {
                    promptBuilder.AppendLine($"- {q.Title} (ID: {q.Id})");
                }
            }

            promptBuilder.AppendLine();

            // Add player context
            promptBuilder.AppendLine("# Player Context");
            promptBuilder.AppendLine($"Player Name: {player.Name}");
            promptBuilder.AppendLine($"Background: {player.Backstory}");
            
            // Add player visual description
            if (player.VisualDescription != null)
            {
                promptBuilder.AppendLine($"Gender: {player.VisualDescription.Gender}");
                promptBuilder.AppendLine($"Body: {player.VisualDescription.Body}");
                promptBuilder.AppendLine($"Clothing: {player.VisualDescription.VisibleClothing}");
                promptBuilder.AppendLine($"Physical Condition: {player.VisualDescription.Condition}");
            }
            
            // Add player rpg elements
            if (player.RpgElements != null && player.RpgElements.Count > 0)
            {
                promptBuilder.AppendLine("RPG Elements:");
                foreach (var element in player.RpgElements)
                {
                    promptBuilder.AppendLine($"- {element.Key}: {element.Value}");
                }
            }
            
            // Add player inventory
            if (player.Inventory != null && player.Inventory.Count > 0)
            {
                promptBuilder.AppendLine("Inventory:");
                foreach (var item in player.Inventory)
                {
                    promptBuilder.AppendLine($"- {item.Name} (x{item.Quantity}): {item.Description}");
                }
            }
            
            // Add player status effects
            if (player.StatusEffects != null && player.StatusEffects.Count > 0)
            {
                promptBuilder.AppendLine($"Status Effects: {string.Join(", ", player.StatusEffects)}");
            }
            
            promptBuilder.AppendLine();

            // Add location context
            promptBuilder.AppendLine("# Current Location");
            promptBuilder.AppendLine($"Location Name: {location.Name}");
            promptBuilder.AppendLine($"Location Type: {location.Type}");
            promptBuilder.AppendLine($"Description: {location.Description}");
            
            // Add connected locations
            if (location.ConnectedLocations != null && location.ConnectedLocations.Count > 0)
            {
                promptBuilder.AppendLine("Connected Locations:");
                foreach (var connectedLocation in location.ConnectedLocations)
                {
                    promptBuilder.AppendLine($"- {connectedLocation.Id}: {connectedLocation.Description}");
                }
            }
            
            // Add sublocations
            if (location.SubLocations != null && location.SubLocations.Count > 0)
            {
                promptBuilder.AppendLine("Sub-Locations:");
                foreach (var subLocation in location.SubLocations)
                {
                    promptBuilder.AppendLine($"- {subLocation.Id}: {subLocation.Description}");
                }
            }
            
            // Add points of interest
            if (location.PointsOfInterest != null && location.PointsOfInterest.Count > 0)
            {
                promptBuilder.AppendLine("Points of Interest:");
                foreach (var poi in location.PointsOfInterest)
                {
                    promptBuilder.AppendLine($"- {poi.Name}: {poi.Description}");
                }
            }
            
            // Add location items
            if (location.Items != null && location.Items.Count > 0)
            {
                promptBuilder.AppendLine($"Items Present: {string.Join(", ", location.Items)}");
            }
            
            promptBuilder.AppendLine();

            // Add NPCs present at this location and all the information about them
            if (npcsInCurrentLocation != null && npcsInCurrentLocation.Count > 0)
            {
                promptBuilder.AppendLine("# NPCs Present");
                foreach (var npc in npcsInCurrentLocation)
                {
                    promptBuilder.AppendLine($"## NPC: {npc.Name} (ID: {npc.Id})");
                    
                    // Add NPC visual description
                    if (npc.VisualDescription != null)
                    {
                        promptBuilder.AppendLine($"Appearance: {npc.VisualDescription.Gender} {npc.VisualDescription.Body} wearing {npc.VisualDescription.VisibleClothing}, {npc.VisualDescription.Condition}");
                    }
                    
                    // Add NPC personality
                    if (npc.Personality != null)
                    {
                        promptBuilder.AppendLine($"Personality: {npc.Personality.Temperament}, {npc.Personality.Quirks}");
                        if (!string.IsNullOrEmpty(npc.Personality.Quirks))
                        {
                            promptBuilder.AppendLine($"Quirks: {npc.Personality.Quirks}");
                        }
                        if (!string.IsNullOrEmpty(npc.Personality.Motivations)) 
                        {
                            promptBuilder.AppendLine($"Motivations: {npc.Personality.Motivations}");
                        }
                        if (!string.IsNullOrEmpty(npc.Personality.Fears))
                        {
                            promptBuilder.AppendLine($"Fears: {npc.Personality.Fears}");
                        }
                        if (npc.Personality.Secrets != null && npc.Personality.Secrets.Count > 0)
                        {
                            promptBuilder.AppendLine($"Secrets: {string.Join(", ", npc.Personality.Secrets)}");
                        }
                    }
                    
                    // Add backstory
                    if (!string.IsNullOrEmpty(npc.Backstory))
                    {
                        promptBuilder.AppendLine($"Backstory: {npc.Backstory}");
                    }
                    
                    // Add disposition towards player
                    if (!string.IsNullOrEmpty(npc.DispositionTowardsPlayer))
                    {
                        promptBuilder.AppendLine($"Disposition: {npc.DispositionTowardsPlayer}");
                    }
                    
                    // Add relevant quest involvement
                    if (npc.QuestInvolvement != null && npc.QuestInvolvement.Count > 0)
                    {
                        promptBuilder.AppendLine($"Quest Involvement: {string.Join(", ", npc.QuestInvolvement)}");
                    }
                    
                    promptBuilder.AppendLine();
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
                    promptBuilder.AppendLine($"## Quest: {quest.Title} (ID: {quest.Id})");
                    promptBuilder.AppendLine($"Description: {quest.QuestDescription}");
                    promptBuilder.AppendLine($"Current Progress: {quest.CurrentProgress}");
                    
                    // Add achievement conditions
                    if (quest.AchievementConditions != null && quest.AchievementConditions.Count > 0)
                    {
                        promptBuilder.AppendLine("Achievement Conditions:");
                        foreach (var condition in quest.AchievementConditions)
                        {
                            promptBuilder.AppendLine($"- {condition}");
                        }
                    }
                    
                    // Add fail conditions
                    if (quest.FailConditions != null && quest.FailConditions.Count > 0)
                    {
                        promptBuilder.AppendLine("Fail Conditions:");
                        foreach (var condition in quest.FailConditions)
                        {
                            promptBuilder.AppendLine($"- {condition}");
                        }
                    }
                    
                    // Add involved locations
                    if (quest.InvolvedLocations != null && quest.InvolvedLocations.Count > 0)
                    {
                        promptBuilder.AppendLine($"Involved Locations: {string.Join(", ", quest.InvolvedLocations)}");
                    }
                    
                    // Add involved NPCs
                    if (quest.InvolvedNpcs != null && quest.InvolvedNpcs.Count > 0)
                    {
                        promptBuilder.AppendLine($"Involved NPCs: {string.Join(", ", quest.InvolvedNpcs)}");
                    }
                    
                    // Add quest log if available
                    if (quest.QuestLog != null && quest.QuestLog.Count > 0)
                    {
                        promptBuilder.AppendLine("Quest Log:");
                        foreach (var entry in quest.QuestLog)
                        {
                            promptBuilder.AppendLine($"- [{entry.Timestamp}] {entry.Event}: {entry.Description}");
                        }
                    }
                    
                    promptBuilder.AppendLine();
                }
            }
            else
            {
                promptBuilder.AppendLine("# Active Quests");
                promptBuilder.AppendLine("The player currently has no active quests.");
                promptBuilder.AppendLine();
            }

            // Add conversation history
            promptBuilder.AppendLine("# Conversation History");

            // Just include the last 10 messages to keep the prompt size reasonable
            var recentMessages = conversationLog.Messages
                .Skip(Math.Max(0, conversationLog.Messages.Count - 10))
                .ToList();

            if (recentMessages.Count > 0)
            {
                foreach (var message in recentMessages)
                {
                    string sender = message.Sender == "user" ? "Player" : "DM";
                    promptBuilder.AppendLine($"{sender}: {message.Content}");
                }
            }
            else
            {
                promptBuilder.AppendLine("No previous conversation.");
            }
            promptBuilder.AppendLine();
            // Add the user's input
            promptBuilder.AppendLine("# Curren player prompt:");
            promptBuilder.AppendLine(userInput);

            return promptBuilder.ToString();
        }

        private async Task<string> BuildNPCPromptAsync(string userId, string npcId, string userInput)
        {
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

            // Add game setting and preferences
            promptBuilder.AppendLine("# Game Setting");
            promptBuilder.AppendLine($"Genre: {gameSetting.Genre}");
            promptBuilder.AppendLine($"Theme: {gameSetting.Theme}");
            promptBuilder.AppendLine($"Description: {gameSetting.Description}");
            promptBuilder.AppendLine();

            // Add game preferences
            promptBuilder.AppendLine("# Game Preferences");
            promptBuilder.AppendLine($"Tone: {gamePreferences.Tone}");
            promptBuilder.AppendLine($"Complexity: {gamePreferences.Complexity}");
            promptBuilder.AppendLine($"Age Appropriateness: {gamePreferences.AgeAppropriateness}");
            promptBuilder.AppendLine();

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
            promptBuilder.AppendLine($"Weather: {world.WorldStateEffects.Weather}");
            promptBuilder.AppendLine();

            // Add player context
            promptBuilder.AppendLine("# Player Context");
            promptBuilder.AppendLine($"Player Name: {player.Name}");
            promptBuilder.AppendLine($"Appearance: {(player.VisualDescription != null ? player.VisualDescription.Body : "Unknown")}");
            
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
            promptBuilder.AppendLine("# Response Instructions");
            promptBuilder.AppendLine(responseInstructions);
            promptBuilder.AppendLine();

            // Add example responses
            promptBuilder.AppendLine("# Example Responses");
            promptBuilder.AppendLine(exampleResponses);
            promptBuilder.AppendLine();

            // Add the user's input
            promptBuilder.AppendLine("# User Input");
            promptBuilder.AppendLine(userInput);

            return promptBuilder.ToString();
        }

        private async Task<string> BuildCreateQuestPromptAsync(string userId, string userInput)
        {
            // Load create quest template files
            var systemPrompt = await _storageService.GetCreateQuestTemplateAsync("SystemCreateQuest");
            var responseInstructions = await _storageService.GetCreateQuestTemplateAsync("ResponseInstructions");
            var exampleResponses = await _storageService.GetCreateQuestTemplateAsync("ExampleResponses");

            // Load player and world data for context
            var player = await _storageService.GetPlayerAsync(userId);
            var world = await _storageService.GetWorldAsync(userId);
            var gameSetting = await _storageService.GetGameSettingAsync(userId);
            var gamePreferences = await _storageService.GetGamePreferencesAsync(userId);

            // Create the final prompt
            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine(systemPrompt);
            promptBuilder.AppendLine();

            // Add game setting and preferences
            promptBuilder.AppendLine("# Game Setting");
            promptBuilder.AppendLine($"Genre: {gameSetting.Genre}");
            promptBuilder.AppendLine($"Theme: {gameSetting.Theme}");
            promptBuilder.AppendLine($"Description: {gameSetting.Description}");
            promptBuilder.AppendLine();

            // Add game preferences
            promptBuilder.AppendLine("# Game Preferences");
            promptBuilder.AppendLine($"Tone: {gamePreferences.Tone}");
            promptBuilder.AppendLine($"Complexity: {gamePreferences.Complexity}");
            promptBuilder.AppendLine($"Age Appropriateness: {gamePreferences.AgeAppropriateness}");
            promptBuilder.AppendLine();

            // Add world context
            promptBuilder.AppendLine("# World Context");
            promptBuilder.AppendLine();

            // Add player context
            promptBuilder.AppendLine("# Player Context");
            promptBuilder.AppendLine($"Player Name: {player.Name}");
            promptBuilder.AppendLine($"Class: {player.RpgElements["class"]}");
            promptBuilder.AppendLine($"Level: {player.RpgElements["level"]}");
            promptBuilder.AppendLine($"Background: {player.Backstory}");
            promptBuilder.AppendLine();

            // Add trigger instructions
            promptBuilder.AppendLine("# Trigger Instructions");
            promptBuilder.AppendLine("This quest is being created based on a specific trigger in the game world.");
            promptBuilder.AppendLine();

            // Add response instructions
            promptBuilder.AppendLine("# Response Instructions");
            promptBuilder.AppendLine(responseInstructions);
            promptBuilder.AppendLine();

            // Add example responses
            promptBuilder.AppendLine("# Example Responses");
            promptBuilder.AppendLine(exampleResponses);
            promptBuilder.AppendLine();

            // Add the user's input
            promptBuilder.AppendLine("# Quest Request");
            promptBuilder.AppendLine(userInput);

            return promptBuilder.ToString();
        }

        private async Task<string> BuildCreateQuestJsonPromptAsync(string userId, string userInput)
        {
            // Load create quest JSON template files
            var systemPrompt = await _storageService.GetCreateQuestJsonTemplateAsync("SystemCreateQuestJson");
            var responseInstructions = await _storageService.GetCreateQuestJsonTemplateAsync("ResponseInstructions");
            var exampleResponses = await _storageService.GetCreateQuestJsonTemplateAsync("ExampleResponses");

            // Create the final prompt
            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine(systemPrompt);
            promptBuilder.AppendLine();

            // Add response instructions
            promptBuilder.AppendLine("# Response Instructions");
            promptBuilder.AppendLine(responseInstructions);
            promptBuilder.AppendLine();

            // Add example responses
            promptBuilder.AppendLine("# Example Responses");
            promptBuilder.AppendLine(exampleResponses);
            promptBuilder.AppendLine();

            // Add the user's input containing the quest description
            promptBuilder.AppendLine("# Quest Description to Convert to JSON");
            promptBuilder.AppendLine(userInput);

            return promptBuilder.ToString();
        }

        private async Task<string> BuildCreateNPCPromptAsync(string userId, string userInput)
        {
            // Load create NPC template files
            var systemPrompt = await _storageService.GetCreateNpcTemplateAsync("SystemCreateNPC");
            var responseInstructions = await _storageService.GetCreateNpcTemplateAsync("ResponseInstructions");
            var exampleResponses = await _storageService.GetCreateNpcTemplateAsync("ExampleResponses");

            // Load world data for context
            var world = await _storageService.GetWorldAsync(userId);
            var gameSetting = await _storageService.GetGameSettingAsync(userId);
            var gamePreferences = await _storageService.GetGamePreferencesAsync(userId);

            // Create the final prompt
            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine(systemPrompt);
            promptBuilder.AppendLine();

            // Add game setting and preferences
            promptBuilder.AppendLine("# Game Setting");
            promptBuilder.AppendLine($"Genre: {gameSetting.Genre}");
            promptBuilder.AppendLine($"Theme: {gameSetting.Theme}");
            promptBuilder.AppendLine($"Description: {gameSetting.Description}");
            promptBuilder.AppendLine();

            // Add game preferences
            promptBuilder.AppendLine("# Game Preferences");
            promptBuilder.AppendLine($"Tone: {gamePreferences.Tone}");
            promptBuilder.AppendLine($"Complexity: {gamePreferences.Complexity}");
            promptBuilder.AppendLine($"Age Appropriateness: {gamePreferences.AgeAppropriateness}");
            promptBuilder.AppendLine();

            // Add world context
            promptBuilder.AppendLine("# World Context");
            promptBuilder.AppendLine();

            // Add trigger instructions
            promptBuilder.AppendLine("# Trigger Instructions");
            promptBuilder.AppendLine("This NPC is being created based on a specific need in the game world.");
            promptBuilder.AppendLine();

            // Add response instructions
            promptBuilder.AppendLine("# Response Instructions");
            promptBuilder.AppendLine(responseInstructions);
            promptBuilder.AppendLine();

            // Add example responses
            promptBuilder.AppendLine("# Example Responses");
            promptBuilder.AppendLine(exampleResponses);
            promptBuilder.AppendLine();

            // Add the user's input
            promptBuilder.AppendLine("# NPC Creation Request");
            promptBuilder.AppendLine(userInput);

            return promptBuilder.ToString();
        }

        private async Task<string> BuildCreateNPCJsonPromptAsync(string userId, string userInput)
        {
            // Load create NPC JSON template files
            var systemPrompt = await _storageService.GetCreateNpcJsonTemplateAsync("SystemCreateNPCJson");
            var responseInstructions = await _storageService.GetCreateNpcJsonTemplateAsync("ResponseInstructions");
            var exampleResponses = await _storageService.GetCreateNpcJsonTemplateAsync("ExampleResponses");

            // Create the final prompt
            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine(systemPrompt);
            promptBuilder.AppendLine();

            // Add response instructions
            promptBuilder.AppendLine("# Response Instructions");
            promptBuilder.AppendLine(responseInstructions);
            promptBuilder.AppendLine();

            // Add example responses
            promptBuilder.AppendLine("# Example Responses");
            promptBuilder.AppendLine(exampleResponses);
            promptBuilder.AppendLine();

            // Add the user's input containing the NPC description
            promptBuilder.AppendLine("# NPC Description to Convert to JSON");
            promptBuilder.AppendLine(userInput);

            return promptBuilder.ToString();
        }

        private async Task<string> BuildCreateLocationPromptAsync(string userId, string userInput)
        {
            // Load create location template files
            var systemPrompt = await _storageService.GetCreateLocationTemplateAsync("SystemCreateLocation");
            var responseInstructions = await _storageService.GetCreateLocationTemplateAsync("ResponseInstructions");
            var exampleResponses = await _storageService.GetCreateLocationTemplateAsync("ExampleResponses");

            // Load world data for context
            var world = await _storageService.GetWorldAsync(userId);
            var gameSetting = await _storageService.GetGameSettingAsync(userId);
            var gamePreferences = await _storageService.GetGamePreferencesAsync(userId);

            // Create the final prompt
            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine(systemPrompt);
            promptBuilder.AppendLine();

            // Add game setting and preferences
            promptBuilder.AppendLine("# Game Setting");
            promptBuilder.AppendLine($"Genre: {gameSetting.Genre}");
            promptBuilder.AppendLine($"Theme: {gameSetting.Theme}");
            promptBuilder.AppendLine($"Description: {gameSetting.Description}");
            promptBuilder.AppendLine();

            // Add game preferences
            promptBuilder.AppendLine("# Game Preferences");
            promptBuilder.AppendLine($"Tone: {gamePreferences.Tone}");
            promptBuilder.AppendLine($"Complexity: {gamePreferences.Complexity}");
            promptBuilder.AppendLine($"Age Appropriateness: {gamePreferences.AgeAppropriateness}");
            promptBuilder.AppendLine();

            // Add world context
            promptBuilder.AppendLine("# World Context");
            promptBuilder.AppendLine($"Summary: {world.Lore[0].Summary}");
            promptBuilder.AppendLine();

            // Add trigger instructions
            promptBuilder.AppendLine("# Trigger Instructions");
            promptBuilder.AppendLine("This location is being created based on a specific need in the game world.");
            promptBuilder.AppendLine();

            // Add response instructions
            promptBuilder.AppendLine("# Response Instructions");
            promptBuilder.AppendLine(responseInstructions);
            promptBuilder.AppendLine();

            // Add example responses
            promptBuilder.AppendLine("# Example Responses");
            promptBuilder.AppendLine(exampleResponses);
            promptBuilder.AppendLine();

            // Add the user's input
            promptBuilder.AppendLine("# Location Creation Request");
            promptBuilder.AppendLine(userInput);

            return promptBuilder.ToString();
        }

        private async Task<string> BuildCreateLocationJsonPromptAsync(string userId, string userInput)
        {
            // Load create location JSON template files
            var systemPrompt = await _storageService.GetCreateLocationJsonTemplateAsync("SystemCreateLocationJson");
            var responseInstructions = await _storageService.GetCreateLocationJsonTemplateAsync("ResponseInstructions");
            var exampleResponses = await _storageService.GetCreateLocationJsonTemplateAsync("ExampleResponses");

            // Load world data for context
            var world = await _storageService.GetWorldAsync(userId);
            var gameSetting = await _storageService.GetGameSettingAsync(userId);
            var gamePreferences = await _storageService.GetGamePreferencesAsync(userId);

            // Create the final prompt
            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine(systemPrompt);
            promptBuilder.AppendLine();

            // Add game setting and preferences
            promptBuilder.AppendLine("# Game Setting");
            promptBuilder.AppendLine($"Genre: {gameSetting.Genre}");
            promptBuilder.AppendLine($"Theme: {gameSetting.Theme}");
            promptBuilder.AppendLine($"Description: {gameSetting.Description}");
            promptBuilder.AppendLine();

            // Add game preferences
            promptBuilder.AppendLine("# Game Preferences");
            promptBuilder.AppendLine($"Tone: {gamePreferences.Tone}");
            promptBuilder.AppendLine($"Complexity: {gamePreferences.Complexity}");
            promptBuilder.AppendLine($"Age Appropriateness: {gamePreferences.AgeAppropriateness}");
            promptBuilder.AppendLine();

            // Add world context
            promptBuilder.AppendLine("# World Context");
            promptBuilder.AppendLine($"Summary: {world.Lore[0].Summary}");
            promptBuilder.AppendLine();

            // Add trigger instructions
            promptBuilder.AppendLine("# Trigger Instructions");
            promptBuilder.AppendLine("This location is being created based on a specific need in the game world.");
            promptBuilder.AppendLine();

            // Add response instructions
            promptBuilder.AppendLine("# Response Instructions");
            promptBuilder.AppendLine(responseInstructions);
            promptBuilder.AppendLine();

            // Add example responses
            promptBuilder.AppendLine("# Example Responses");
            promptBuilder.AppendLine(exampleResponses);
            promptBuilder.AppendLine();

            // Add the user's input
            promptBuilder.AppendLine("# Location Description to Convert to JSON");
            promptBuilder.AppendLine(userInput);

            return promptBuilder.ToString();
        }

        private async Task<string> BuildCreatePlayerJsonPromptAsync(string userId, string userInput)
        {
            // Load create player JSON template files
            var systemPrompt = await _storageService.GetCreatePlayerJsonTemplateAsync("SystemCreatePlayerJson");
            var exampleResponses = await _storageService.GetCreatePlayerJsonTemplateAsync("ExampleResponses");

            // Load world data for context
            var world = await _storageService.GetWorldAsync(userId);
            var gameSetting = await _storageService.GetGameSettingAsync(userId);
            var gamePreferences = await _storageService.GetGamePreferencesAsync(userId);

            // Create the final prompt
            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine(systemPrompt);
            promptBuilder.AppendLine();

            // Add example responses
            promptBuilder.AppendLine("# Example Prompts and Responses");
            promptBuilder.AppendLine(exampleResponses);
            promptBuilder.AppendLine();

            // Add game setting and preferences
            promptBuilder.AppendLine("# Game Setting");
            promptBuilder.AppendLine($"Genre: {gameSetting.Genre}");
            promptBuilder.AppendLine($"Theme: {gameSetting.Theme}");
            promptBuilder.AppendLine($"Description: {gameSetting.Description}");
            promptBuilder.AppendLine($"Starting location: {gameSetting.StartingLocation}");
            promptBuilder.AppendLine();

            // Add game preferences
            promptBuilder.AppendLine("# Game Preferences");
            promptBuilder.AppendLine($"Tone: {gamePreferences.Tone}");
            promptBuilder.AppendLine($"Complexity: {gamePreferences.Complexity}");
            promptBuilder.AppendLine($"Age Appropriateness: {gamePreferences.AgeAppropriateness}");
            promptBuilder.AppendLine();

            // Add world context
            promptBuilder.AppendLine("# World Context");
            promptBuilder.AppendLine($"Player ID: {userId}");
            promptBuilder.AppendLine();

            // Add the user's input containing the player description
            promptBuilder.AppendLine("# Player Description to Convert to JSON");
            promptBuilder.AppendLine(userInput);

            return promptBuilder.ToString();
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
