using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using AiGMBackEnd.Models;
using System.Collections.Generic;

namespace AiGMBackEnd.Hubs
{
    public class GameHub : Hub
    {
        /// <summary>
        /// Send a notification that inventory has changed for a specific game
        /// </summary>
        /// <param name="gameId">The game ID</param>
        public async Task NotifyInventoryChanged(string gameId)
        {
            await Clients.Group(gameId).SendAsync("InventoryChanged", gameId);
        }
        
        /// <summary>
        /// Allow clients to join a game-specific group to receive updates.
        /// </summary>
        /// <param name="gameId">The game ID to join</param>
        public async Task JoinGame(string gameId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
        }
        
        /// <summary>
        /// Allow clients to leave a game-specific group.
        /// </summary>
        /// <param name="gameId">The game ID to leave</param>
        public async Task LeaveGame(string gameId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameId);
        }
    }
} 