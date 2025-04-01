using AiGMBackEnd.Models.Prompts;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using AiGMBackEnd.Services.Processors;
using System.IO;
using static AiGMBackEnd.Services.StorageService;
using Newtonsoft.Json;

namespace AiGMBackEnd.Services
{
    public class PresenterService
    {
        private readonly PromptService _promptService;
        private readonly ResponseProcessingService _responseProcessingService;
        private readonly BackgroundJobService _backgroundJobService;
        private readonly LoggingService _loggingService;
        private readonly StorageService _storageService;
        private readonly UpdateProcessor _updateProcessor;

        public PresenterService(
            PromptService promptService,
            ResponseProcessingService responseProcessingService,
            BackgroundJobService backgroundJobService,
            LoggingService loggingService,
            StorageService storageService)
        {
            _promptService = promptService;
            _responseProcessingService = responseProcessingService;
            _backgroundJobService = backgroundJobService;
            _loggingService = loggingService;
            _storageService = storageService;
            _updateProcessor = new UpdateProcessor(storageService, loggingService, backgroundJobService);
        }

        public async Task<string> HandleUserInputAsync(string userId, string userInput, PromptType promptType = PromptType.DM, string npcId = null)
        {
            try
            {
                _loggingService.LogInfo($"Handling input for user {userId} with promptType {promptType}: {userInput}");

                var prompt = new PromptRequest
                {
                    PromptType = promptType,
                    UserId = userId,
                    UserInput = userInput,
                    NpcId = npcId
                };
                
                var response = await _backgroundJobService.EnqueuePromptAsync(prompt);
                
                _loggingService.LogInfo($"Completed handling input for user {userId}");
                
                return response;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error handling user input: {ex.Message}");
                return $"Error processing your request: {ex.Message}";
            }
        }

        public async Task<int> AutoCreateDanglingReferencesAsync(string userId)
        {
            try
            {
                _loggingService.LogInfo($"Finding and auto-creating dangling references for user {userId}");
                
                // Get all dangling references
                var danglingReferences = await _storageService.FindDanglingReferencesAsync(userId);
                int createdCount = 0;
                
                if (danglingReferences == null || danglingReferences.Count == 0)
                {
                    _loggingService.LogInfo($"No dangling references found for user {userId}");
                    return 0;
                }
                
                _loggingService.LogInfo($"Found {danglingReferences.Count} dangling references for user {userId}");
                
                // Process each dangling reference
                foreach (var reference in danglingReferences)
                {
                    // Get context information based on the reference type and file path
                    string contextMessage = $"This is automatically created request to produce {reference.ReferenceType}.";
                    string sourceContent = string.Empty;
                    
                    // Extract entity ID from the filepath if possible
                    string sourceEntityId = Path.GetFileNameWithoutExtension(reference.FilePath);
                    string sourceEntityType = GetEntityTypeFromPath(reference.FilePath);
                    
                    // Try to get source entity data based on file path
                    if (!string.IsNullOrEmpty(sourceEntityId))
                    {
                        try
                        {
                            switch (sourceEntityType.ToLower())
                            {
                                case "npcs":
                                    var npc = await _storageService.GetNpcAsync(userId, sourceEntityId);
                                    if (npc != null)
                                    {
                                        contextMessage += $"This reference was found in NPC '{npc.Name}' with ID {sourceEntityId}. Find where in this NPC context is the {sourceEntityId} and create it in such a way that it would be relevant to this NPC.";
                                        sourceContent = JsonConvert.SerializeObject(npc, Formatting.Indented);
                                    }
                                    break;
                                    
                                case "locations":
                                    var location = await _storageService.GetLocationAsync(userId, sourceEntityId);
                                    if (location != null)
                                    {
                                        contextMessage += $"This reference was found in Location '{location.Name}' with ID {sourceEntityId}.  Find where in this Location context is the {sourceEntityId} and create it in such a way that it would be relevant to this Location.";
                                        sourceContent = JsonConvert.SerializeObject(location, Formatting.Indented);
                                    }
                                    break;
                                    
                                case "quests":
                                    var quest = await _storageService.GetQuestAsync(userId, sourceEntityId);
                                    if (quest != null)
                                    {
                                        contextMessage += $"This reference was found in Quest '{quest.Title}' with ID {sourceEntityId}. Find where in this Quest context is the {sourceEntityId} and create it in such a way that it would be relevant to this Quest.";
                                        sourceContent = JsonConvert.SerializeObject(quest, Formatting.Indented);
                                    }
                                    break;
                                    
                                default:
                                    // Check if it's a player or world file
                                    if (sourceEntityId.ToLower() == "player")
                                    {
                                        var player = await _storageService.GetPlayerAsync(userId);
                                        if (player != null)
                                        {
                                            contextMessage += $"This reference was found in the Player data. ";
                                            sourceContent = JsonConvert.SerializeObject(player, Formatting.Indented);
                                        }
                                    }
                                    else if (sourceEntityId.ToLower() == "world")
                                    {
                                        var world = await _storageService.GetWorldAsync(userId);
                                        if (world != null)
                                        {
                                            contextMessage += $"This reference was found in the World data. ";
                                            sourceContent = JsonConvert.SerializeObject(world, Formatting.Indented);
                                        }
                                    }
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            _loggingService.LogWarning($"Error retrieving source entity data: {ex.Message}");
                        }
                    }
                    
                    // Add the source content to context if available
                    if (!string.IsNullOrEmpty(sourceContent))
                    {
                        contextMessage += $"\n\nHere is the full content of the source entity:\n\n{sourceContent}";
                    }
                    
                    // Create prompt request based on entity type
                    PromptRequest request = null;
                    
                    switch (reference.ReferenceType.ToUpper())
                    {
                        case "NPC":
                            request = new PromptRequest
                            {
                                PromptType = PromptType.CreateNPC,
                                UserId = userId,
                                NpcId = reference.ReferenceId,
                                NpcName = reference.ReferenceId.Replace("npc_", "").Replace("_", " "),
                                Context = contextMessage
                            };
                            break;
                        
                        case "LOCATION":
                            request = new PromptRequest
                            {
                                PromptType = PromptType.CreateLocation,
                                UserId = userId,
                                LocationId = reference.ReferenceId,
                                LocationType = "BUILDING",
                                LocationName = reference.ReferenceId.Replace("loc_", "").Replace("_", " "),
                                Context = contextMessage
                            };
                            break;
                        
                        case "QUEST":
                            request = new PromptRequest
                            {
                                PromptType = PromptType.CreateQuest,
                                UserId = userId,
                                QuestId = reference.ReferenceId,
                                QuestName = reference.ReferenceId.Replace("quest_", "").Replace("_", " "),
                                Context = contextMessage
                            };
                            break;
                        
                        default:
                            _loggingService.LogWarning($"Unknown reference type: {reference.ReferenceType}");
                            continue;
                    }
                    
                    if (request != null)
                    {
                        _loggingService.LogInfo($"Creating entity for dangling reference: {reference.ReferenceId} of type {reference.ReferenceType}");
                        _updateProcessor.FireAndForgetEntityCreation(request);
                        createdCount++;
                    }
                }
                
                return createdCount;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error auto-creating dangling references for user {userId}: {ex.Message}");
                throw;
            }
        }

        // Helper method to extract entity type from file path
        private string GetEntityTypeFromPath(string filePath)
        {
            string[] parts = filePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            
            // Find the relevant directory in the path
            foreach (var part in parts)
            {
                if (part.Equals("npcs", StringComparison.OrdinalIgnoreCase) ||
                    part.Equals("locations", StringComparison.OrdinalIgnoreCase) ||
                    part.Equals("quests", StringComparison.OrdinalIgnoreCase))
                {
                    return part;
                }
            }
            
            return string.Empty;
        }
    }
}
