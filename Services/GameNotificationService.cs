using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using AiGMBackEnd.Hubs;
using AiGMBackEnd.Models;
using System.Collections.Generic;

namespace AiGMBackEnd.Services
{
    public class GameNotificationService
    {
        private readonly IHubContext<GameHub> _hubContext;
        private readonly LoggingService _loggingService;

        public GameNotificationService(IHubContext<GameHub> hubContext, LoggingService loggingService)
        {
            _hubContext = hubContext;
            _loggingService = loggingService;
        }

        /// <summary>
        /// Notify clients that inventory has been updated and they should fetch the latest
        /// </summary>
        /// <param name="gameId">The game ID</param>
        public async Task NotifyInventoryChangedAsync(string gameId)
        {
            try
            {
                await _hubContext.Clients.Group(gameId).SendAsync("InventoryChanged", gameId);
                _loggingService.LogInfo($"Sent inventory change notification for game {gameId}");
            }
            catch (System.Exception ex)
            {
                _loggingService.LogError($"Error sending inventory change notification: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Notify clients that combat has started and they should switch to combat UI mode
        /// </summary>
        /// <param name="gameId">The game ID</param>
        /// <param name="initialState">Basic information about the combat encounter</param>
        public async Task NotifyCombatStartedAsync(string gameId, CombatStartInfo initialState)
        {
            try
            {
                await _hubContext.Clients.Group(gameId).SendAsync("CombatStarted", initialState);
                _loggingService.LogInfo($"Sent combat start notification for game {gameId}. Enemy: {initialState.EnemyName}");
            }
            catch (System.Exception ex)
            {
                _loggingService.LogError($"Error sending combat start notification: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Notify clients that combat has ended and they should switch back to normal UI mode
        /// </summary>
        /// <param name="gameId">The game ID</param>
        /// <param name="playerVictory">Whether the player won the combat (true) or was defeated (false)</param>
        public async Task NotifyCombatEndedAsync(string gameId, bool playerVictory)
        {
            try
            {
                await _hubContext.Clients.Group(gameId).SendAsync("CombatEnded", new { playerVictory });
                _loggingService.LogInfo($"Sent combat end notification for game {gameId}. Player victory: {playerVictory}");
            }
            catch (System.Exception ex)
            {
                _loggingService.LogError($"Error sending combat end notification: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Optional: Notify clients about updates to the combat state during a turn
        /// </summary>
        /// <param name="gameId">The game ID</param>
        /// <param name="currentState">Information about the current combat state</param>
        public async Task NotifyCombatTurnUpdateAsync(string gameId, CombatTurnInfo currentState)
        {
            try
            {
                await _hubContext.Clients.Group(gameId).SendAsync("CombatTurnUpdate", currentState);
                _loggingService.LogInfo($"Sent combat turn update for game {gameId}. Successes: {currentState.CurrentEnemySuccesses}/{currentState.SuccessesRequired}");
            }
            catch (System.Exception ex)
            {
                _loggingService.LogError($"Error sending combat turn update: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Send a generic notification to clients with a custom message
        /// </summary>
        /// <param name="gameId">The game ID</param>
        /// <param name="message">The message to send to clients</param>
        public async Task NotifyGenericAsync(string gameId, string message)
        {
            try
            {
                await _hubContext.Clients.Group(gameId).SendAsync("GenericNotification", new { message });
                _loggingService.LogInfo($"Sent generic notification for game {gameId}: {message}");
            }
            catch (System.Exception ex)
            {
                _loggingService.LogError($"Error sending generic notification: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Send an error notification to clients with an error message
        /// </summary>
        /// <param name="gameId">The game ID</param>
        /// <param name="errorMessage">The error message to send to clients</param>
        public async Task NotifyErrorAsync(string gameId, string errorMessage)
        {
            try
            {
                await _hubContext.Clients.Group(gameId).SendAsync("ErrorNotification", new { errorMessage });
                _loggingService.LogInfo($"Sent error notification for game {gameId}: {errorMessage}");
            }
            catch (System.Exception ex)
            {
                _loggingService.LogError($"Error sending error notification: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Notify clients that the player's location has changed and they should update accordingly
        /// </summary>
        /// <param name="gameId">The game ID</param>
        public async Task NotifyLocationChangedAsync(string gameId)
        {
            try
            {
                await _hubContext.Clients.Group(gameId).SendAsync("LocationChanged", gameId);
                _loggingService.LogInfo($"Sent location change notification for game {gameId}: LocationChanged");
            }
            catch (System.Exception ex)
            {
                _loggingService.LogError($"Error sending location change notification: {ex.Message}");
            }
        }
    }
} 