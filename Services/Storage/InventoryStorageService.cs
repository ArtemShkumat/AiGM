using System;
using System.Linq;
using System.Threading.Tasks;
using AiGMBackEnd.Models;

namespace AiGMBackEnd.Services.Storage
{
    public class InventoryStorageService : IInventoryStorageService
    {
        private readonly StorageService _storageService;
        private readonly LoggingService _loggingService;

        public InventoryStorageService(
            StorageService storageService,
            LoggingService loggingService)
        {
            _storageService = storageService;
            _loggingService = loggingService;
        }

        public async Task<bool> AddItemToPlayerInventoryAsync(string userId, InventoryItem newItem)
        {
            try
            {
                var player = await _storageService.GetPlayerAsync(userId);
                if (player == null)
                {
                    _loggingService.LogError($"Failed to add item to inventory: Player not found for {userId}");
                    return false;
                }

                // Check if the item already exists in the inventory
                var existingItem = player.Inventory.FirstOrDefault(i => i.Name.Equals(newItem.Name, StringComparison.OrdinalIgnoreCase));

                if (existingItem != null)
                {
                    // If the item exists, increase its quantity
                    existingItem.Quantity += newItem.Quantity;
                }
                else
                {
                    // If the item doesn't exist, add it to the inventory
                    player.Inventory.Add(newItem);
                }

                // Save the updated player
                await _storageService.SaveAsync(userId, "player", player);
                _loggingService.LogInfo($"Added {newItem.Quantity} {newItem.Name} to player inventory for {userId}");
                
                return true;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error adding item to player inventory: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RemoveItemFromPlayerInventoryAsync(string userId, string itemName, int quantity = 1)
        {
            try
            {
                var player = await _storageService.GetPlayerAsync(userId);
                if (player == null)
                {
                    _loggingService.LogError($"Failed to remove item from inventory: Player not found for {userId}");
                    return false;
                }

                // Look for the item in the inventory
                var existingItem = player.Inventory.FirstOrDefault(i => i.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase));

                if (existingItem == null)
                {
                    _loggingService.LogWarning($"Failed to remove item: {itemName} not found in player inventory for {userId}");
                    return false;
                }

                if (existingItem.Quantity <= quantity)
                {
                    // If we're removing all or more than available, remove the item completely
                    player.Inventory.Remove(existingItem);
                    _loggingService.LogInfo($"Removed item {itemName} completely from player inventory for {userId}");
                }
                else
                {
                    // Otherwise, decrease the quantity
                    existingItem.Quantity -= quantity;
                    _loggingService.LogInfo($"Removed {quantity} {itemName} from player inventory for {userId}");
                }

                // Save the updated player
                await _storageService.SaveAsync(userId, "player", player);
                return true;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error removing item from player inventory: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> AddCurrencyAmountAsync(string userId, string currencyName, int amount)
        {
            try
            {
                var player = await _storageService.GetPlayerAsync(userId);
                if (player == null)
                {
                    _loggingService.LogError($"Failed to add currency: Player not found for {userId}");
                    return false;
                }

                // Check if the currency already exists
                var existingCurrency = player.Currencies.FirstOrDefault(c => c.Name.Equals(currencyName, StringComparison.OrdinalIgnoreCase));

                if (existingCurrency != null)
                {
                    // If the currency exists, increase its amount
                    existingCurrency.Amount += amount;
                }
                else
                {
                    // If the currency doesn't exist, add it to the currencies list
                    player.Currencies.Add(new Currency
                    {
                        Name = currencyName,
                        Amount = amount
                    });
                }

                // Save the updated player
                await _storageService.SaveAsync(userId, "player", player);
                _loggingService.LogInfo($"Added {amount} {currencyName} to player currencies for {userId}");
                
                return true;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error adding currency to player: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RemoveCurrencyAmountAsync(string userId, string currencyName, int amount)
        {
            try
            {
                var player = await _storageService.GetPlayerAsync(userId);
                if (player == null)
                {
                    _loggingService.LogError($"Failed to remove currency: Player not found for {userId}");
                    return false;
                }

                // Look for the currency
                var existingCurrency = player.Currencies.FirstOrDefault(c => c.Name.Equals(currencyName, StringComparison.OrdinalIgnoreCase));

                if (existingCurrency == null)
                {
                    _loggingService.LogWarning($"Failed to remove currency: {currencyName} not found for player {userId}");
                    return false;
                }

                if (existingCurrency.Amount < amount)
                {
                    _loggingService.LogWarning($"Not enough {currencyName} to remove {amount} for player {userId}");
                    return false;
                }

                existingCurrency.Amount -= amount;
                
                // If the currency amount is now zero, consider removing it from the list
                if (existingCurrency.Amount == 0)
                {
                    player.Currencies.Remove(existingCurrency);
                }

                // Save the updated player
                await _storageService.SaveAsync(userId, "player", player);
                _loggingService.LogInfo($"Removed {amount} {currencyName} from player currencies for {userId}");
                
                return true;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error removing currency from player: {ex.Message}");
                return false;
            }
        }
    }
} 