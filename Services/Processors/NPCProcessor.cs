using System;
using System.Collections.Generic; // Keep for ConversationLog
using System.Threading.Tasks;
using AiGMBackEnd.Models;
using AiGMBackEnd.Services;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json; // Added for deserialization
using System.IO;
using AiGMBackEnd.Services.Storage; // Add this using statement

namespace AiGMBackEnd.Services.Processors
{
    public class NPCProcessor : INPCProcessor
    {
        private readonly StorageService _storageService;
        private readonly LoggingService _loggingService;
        private readonly IGameScenarioService _gameScenarioService; // Add this field

        public NPCProcessor(
            StorageService storageService,
            LoggingService loggingService,
            IGameScenarioService gameScenarioService) // Add IGameScenarioService here
        {
            _storageService = storageService;
            _loggingService = loggingService;
            _gameScenarioService = gameScenarioService; // Assign it here
        }

        public async Task ProcessAsync(JObject npcData, string userId)
        {
            try
            {
                _loggingService.LogInfo("Processing NPC creation from LLM response.");

                // Check if this is for a starting scenario
                bool isStartingScenario = false;
                var metadata = npcData["metadata"] as JObject;
                if (metadata != null && metadata["isStartingScenario"] != null)
                {
                    bool.TryParse(metadata["isStartingScenario"].ToString(), out isStartingScenario);
                }

                // Extract basic properties
                string npcId = npcData["id"]?.ToString();
                if (string.IsNullOrEmpty(npcId))
                {
                    _loggingService.LogError("Missing npcId in NPC data");
                    return;
                }

                string npcType = npcData["type"]?.ToString() ?? "NPC";
                npcData["type"] = npcType; // Ensure type is set

                // Choose the correct storage method based on whether this is a starting scenario
                if (isStartingScenario)
                {
                    // Get scenarioId from metadata
                    string scenarioId = metadata?["scenarioId"]?.ToString();
                    if (string.IsNullOrEmpty(scenarioId))
                    {
                        _loggingService.LogError($"Missing scenarioId in metadata for starting scenario NPC {npcId}");
                        return;
                    }

                    // Delegate saving to GameScenarioService
                    await _gameScenarioService.SaveScenarioNpcAsync(scenarioId, npcId, npcData, userId, isStartingScenario);
                    // string npcPath = Path.Combine("Data", "startingScenarios", scenarioId, "npcs", $"{npcId}.json");
                    // await File.WriteAllTextAsync(npcPath, npcData.ToString());
                    // _loggingService.LogInfo($"Saved starting scenario NPC {npcId} to {npcPath}");
                }
                else
                {
                    // Normal user save
                    await _storageService.SaveAsync(userId, "npc", npcData);
                    _loggingService.LogInfo($"Successfully processed and saved NPC {npcId} for user {userId}");
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error processing NPC creation: {ex.Message}");
                throw;
            }
        }
    }
} 