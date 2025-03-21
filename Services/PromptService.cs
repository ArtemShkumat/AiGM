using AiGMBackEnd.Models;
using System.Text;

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

            // Load current location
            var location = await _storageService.GetLocationAsync(userId, player.CurrentLocationId);

            // Get NPCs in current location
            var npcSummaries = new List<string>();
            foreach (var npcId in location.Npcs)
            {
                try
                {
                    var npc = await _storageService.GetNpcAsync(userId, npcId);
                    if (npc!=null)
                    {
                        npcSummaries.Add($"NPC ID: {npc.Id}, Name: {npc.Name}, Summary: {npc.Backstory}");
                    }                    
                }
                catch (Exception ex)
                {
                    _loggingService.LogWarning($"Failed to load NPC {npcId}: {ex.Message}");
                }
            }

            // Get active quests
            var activeQuestSummaries = new List<string>();
            foreach (var questId in player.ActiveQuests)
            {
                try
                {
                    var quest = await _storageService.GetQuestAsync(userId, questId);
                    if (quest != null) { activeQuestSummaries.Add($"Quest ID: {quest.Id}, Title: {quest.Title}, Current Step: {quest.CurrentProgress}, Summary: {quest.QuestDescription}"); }                    
                }
                catch (Exception ex)
                {
                    _loggingService.LogWarning($"Failed to load quest {questId}: {ex.Message}");
                }
            }

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
            promptBuilder.AppendLine($"World Name: {world.GameName}");
            promptBuilder.AppendLine($"Setting: {world.Setting}");
            promptBuilder.AppendLine($"Current Time: {world.GameTime}");
            promptBuilder.AppendLine($"Current Weather: {world.WorldStateEffects.Weather}");
            if (world.Lore.Count > 0 && world.Lore[0] != null)
            {
                promptBuilder.AppendLine($"World Summary: {world.Lore[0].Summary}");
            }
            promptBuilder.AppendLine();

            // Add player context
            promptBuilder.AppendLine("# Player Context");
            promptBuilder.AppendLine($"Player Name: {player.Name}");
            promptBuilder.AppendLine($"Background: {player.Backstory}");
            promptBuilder.AppendLine();

            // Add location context
            promptBuilder.AppendLine("# Current Location");
            promptBuilder.AppendLine($"Location Name: {location.Name}");
            promptBuilder.AppendLine($"Location Type: {location.Type}");
            promptBuilder.AppendLine($"Description: {location.Description}");
            promptBuilder.AppendLine($"Time of Day: {world.GameTime}");
            promptBuilder.AppendLine();

            // Add NPCs
            if (npcSummaries.Count > 0)
            {
                promptBuilder.AppendLine("# NPCs in current location");
                foreach (var npcSummary in npcSummaries)
                {
                    promptBuilder.AppendLine(npcSummary);
                }
                promptBuilder.AppendLine();
            }

            // Add active quests
            if (activeQuestSummaries.Count > 0)
            {
                promptBuilder.AppendLine("# Active Quests");
                foreach (var questSummary in activeQuestSummaries)
                {
                    promptBuilder.AppendLine(questSummary);
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
                promptBuilder.AppendLine($"Age: {npc.VisualDescription.BodyType}");
            }
            if (npc.KnownEntities != null)
            {
                promptBuilder.AppendLine($"Role: {npc.Personality.Traits}");
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
            promptBuilder.AppendLine($"Appearance: {(player.VisualDescription != null ? player.VisualDescription.BodyType : "Unknown")}");
            
            // Add NPC's relationship with player if it exists
            var playerRelationship = npc.Relationships.Find(r => r.NpcId == player.Id);
            if (playerRelationship != null)
            {
                promptBuilder.AppendLine($"Relationship with player: {playerRelationship.RelationshipType}");
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
            promptBuilder.AppendLine($"World Name: {world.GameName}");
            promptBuilder.AppendLine($"Setting: {world.Setting}");
            promptBuilder.AppendLine($"Summary: {world.Lore[0].Summary}");
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
            promptBuilder.AppendLine($"World Name: {world.GameName}");
            promptBuilder.AppendLine($"Setting: {world.Setting}");
            promptBuilder.AppendLine($"Summary: {world.Lore[0].Summary}");
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
            promptBuilder.AppendLine($"World Name: {world.GameName}");
            promptBuilder.AppendLine($"Setting: {world.Setting}");
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
            promptBuilder.AppendLine($"World Name: {world.GameName}");
            promptBuilder.AppendLine($"Setting: {world.Setting}");
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
            promptBuilder.AppendLine($"World Name: {world.GameName}");
            promptBuilder.AppendLine($"Setting: {world.Setting}");
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
