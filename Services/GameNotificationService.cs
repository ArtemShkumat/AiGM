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
    }
} 