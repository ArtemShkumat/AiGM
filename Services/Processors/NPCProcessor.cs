using System;
using System.Collections.Generic; // Keep for ConversationLog
using System.Threading.Tasks;
using AiGMBackEnd.Models;
using AiGMBackEnd.Services;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json; // Added for deserialization

namespace AiGMBackEnd.Services.Processors
{
    public class NPCProcessor : INPCProcessor
    {
        private readonly StorageService _storageService;
        private readonly LoggingService _loggingService;

        public NPCProcessor(
            StorageService storageService,
            LoggingService loggingService)
        {
            _storageService = storageService;
            _loggingService = loggingService;
        }

        public async Task ProcessAsync(JObject npcData, string userId)
        {
            try
            {
                _loggingService.LogInfo("Processing NPC creation using direct deserialization.");

                // Directly deserialize JObject to Npc model
                var npc = npcData.ToObject<Models.Npc>();

                // Basic validation
                if (npc == null || string.IsNullOrEmpty(npc.Id))
                {
                    _loggingService.LogError("Failed to deserialize NPC data or NPC ID is missing.");
                    string? potentialId = npcData["id"]?.ToString();
                    _loggingService.LogWarning($"Attempted NPC creation for ID (from JObject): {potentialId ?? "Not Found"}");
                    return;
                }

                // Ensure the Type is set correctly
                if (string.IsNullOrEmpty(npc.Type) || npc.Type != "NPC")
                {
                    _loggingService.LogWarning($"NPC type mismatch or missing for NPC {npc.Id}. Setting to 'NPC'.");
                    npc.Type = "NPC";
                }
                

                // Save the deserialized NPC data
                // Assuming the path format is correct
                await _storageService.SaveAsync(userId, $"npcs/{npc.Id}", npc);
                _loggingService.LogInfo($"Successfully processed and saved NPC: {npc.Id}");

            }
            catch (JsonSerializationException jsonEx)
            {
                _loggingService.LogError($"JSON deserialization error processing NPC creation: {jsonEx.Message}");
                _loggingService.LogInfo($"Problematic JSON data: {npcData.ToString()}"); // Use LogInfo
                throw;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error processing NPC creation: {ex.Message}");
                _loggingService.LogInfo($"JSON data during error: {npcData.ToString()}"); // Use LogInfo
                throw;
            }
        }
    }
} 