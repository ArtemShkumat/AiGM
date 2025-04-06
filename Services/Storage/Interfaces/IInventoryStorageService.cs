using System.Threading.Tasks;
using AiGMBackEnd.Models;

namespace AiGMBackEnd.Services.Storage
{
    public interface IInventoryStorageService
    {
        Task<bool> AddItemToPlayerInventoryAsync(string userId, InventoryItem item);
        Task<bool> RemoveItemFromPlayerInventoryAsync(string userId, string itemName, int quantity = 1);
        Task<bool> AddCurrencyAmountAsync(string userId, string currencyName, int amount);
        Task<bool> RemoveCurrencyAmountAsync(string userId, string currencyName, int amount);
    }
} 