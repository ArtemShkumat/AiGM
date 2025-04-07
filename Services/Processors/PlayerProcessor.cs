using System.Threading.Tasks;
using AiGMBackEnd.Models;
using AiGMBackEnd.Services;
using Newtonsoft.Json.Linq;
using System;
using Newtonsoft.Json;

namespace AiGMBackEnd.Services.Processors
{
    public class PlayerProcessor : IPlayerProcessor
    {
        private readonly StorageService _storageService;
        private readonly LoggingService _loggingService;

        public PlayerProcessor(
            StorageService storageService,
            LoggingService loggingService)
        {
            _storageService = storageService;
            _loggingService = loggingService;
        }

        public async Task ProcessAsync(JObject playerData, string userId)
        {
            try
            {
                _loggingService.LogInfo("Processing player creation using direct deserialization.");

                // Directly deserialize JObject to Player model using Newtonsoft.Json
                // Ensure Player model has appropriate JsonProperty attributes if names differ
                var player = playerData.ToObject<Models.Player>();

                // Basic validation after deserialization
                if (player == null || string.IsNullOrEmpty(player.Id))
                {
                    _loggingService.LogError("Failed to deserialize player data or Player ID is missing.");
                    // Optionally attempt to extract ID from JObject if needed for logging
                    string? potentialId = playerData["id"]?.ToString();
                    _loggingService.LogWarning($"Attempted player creation for ID (from JObject): {potentialId ?? "Not Found"}");
                    return; 
                }
                
                // Ensure the Type is set correctly if not handled by deserialization (though it should be)
                if (string.IsNullOrEmpty(player.Type) || player.Type != "Player")
                {
                    _loggingService.LogWarning($"Player type mismatch or missing for player {player.Id}. Setting to 'Player'.");
                    player.Type = "Player";
                }

                // Assign userId if it's not part of the LLM response (or override if needed)
                // Assuming Player model doesn't have a UserId property and we use the provided userId for storage path.

                // Save the deserialized player data
                // Ensure SaveAsync can handle the Player type directly
                await _storageService.SaveAsync(userId, "player", player); 
                _loggingService.LogInfo($"Successfully processed and saved player: {player.Id}");

            }
            catch (JsonSerializationException jsonEx)
            {
                _loggingService.LogError($"JSON deserialization error processing player creation: {jsonEx.Message}");
                // Log the problematic JSON if possible and not too large/sensitive
                _loggingService.LogInfo($"Problematic JSON data: {playerData.ToString()}");
                throw; // Re-throw to indicate failure
            }
            catch (Exception ex)
            {                
                _loggingService.LogError($"Error processing player creation: {ex.Message}");
                // Log the JSON data for debugging other potential errors
                 _loggingService.LogInfo($"JSON data during error: {playerData.ToString()}");
                throw; // Re-throw to indicate failure
            }
        }
    }
} 