using System.Threading.Tasks;
using AiGMBackEnd.Models;
using AiGMBackEnd.Models.Prompts;
using AiGMBackEnd.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Threading;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using AiGMBackEnd.Services.Storage;
using System.Text.Json;
using System.Text.Json.Serialization;

// Check which interface is being used
using AiGMBackEnd.Services.Processors; // This will use IUpdateProcessor from Services/Processors

namespace AiGMBackEnd.Services.Processors
{
    public class UpdateProcessor : IUpdateProcessor
    {
        private readonly StorageService _storageService;
        private readonly LoggingService _loggingService;
        private readonly IStatusTrackingService _statusTrackingService;
        private readonly ILocationProcessor _locationProcessor;
        private readonly IQuestProcessor _questProcessor;
        private readonly INPCProcessor _npcProcessor;
        private readonly IServiceProvider _serviceProvider;
        private readonly GameNotificationService _notificationService;
        private readonly IInventoryStorageService _inventoryStorageService;
        private readonly JsonSerializerOptions _jsonSerializerOptions;

        public UpdateProcessor(
            StorageService storageService,
            LoggingService loggingService,
            IStatusTrackingService statusTrackingService,
            IServiceProvider serviceProvider,
            ILocationProcessor locationProcessor,
            IQuestProcessor questProcessor,
            INPCProcessor npcProcessor,
            GameNotificationService notificationService,
            IInventoryStorageService inventoryStorageService)
        {
            _storageService = storageService;
            _loggingService = loggingService;
            _statusTrackingService = statusTrackingService;
            _serviceProvider = serviceProvider;
            _locationProcessor = locationProcessor;
            _questProcessor = questProcessor;
            _npcProcessor = npcProcessor;
            _notificationService = notificationService;
            _inventoryStorageService = inventoryStorageService;

            _jsonSerializerOptions = new JsonSerializerOptions
            {
                // Ignore null properties during serialization for the update patch
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                IgnoreReadOnlyProperties = false,
                WriteIndented = true,
                PropertyNameCaseInsensitive = true // Keep this for flexibility if needed, though less critical for serialization
            };
        }

        public async Task ProcessUpdatesAsync(List<ICreationHook> newEntities, Dictionary<string, IUpdatePayload> partialUpdates, string userId)
        {
            try
            {
                _loggingService.LogInfo("Processing updates with strongly-typed data");
                
                var entityCreationJobs = new Dictionary<string, string>();
                
                // Step 1: Process new entities
                if (newEntities != null && newEntities.Any())
                {
                    entityCreationJobs = await ProcessNewEntitiesAsync(newEntities, userId);
                }
                
                // Step 2: Process partial updates
                if (partialUpdates != null && partialUpdates.Any())
                {
                    await ProcessPartialUpdatesAsync(partialUpdates, userId, entityCreationJobs);
                }
                
                _loggingService.LogInfo("Strongly-typed updates processing complete");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error processing strongly-typed updates: {ex.Message}\\nStackTrace: {ex.StackTrace}");
                throw;
            }
        }

        private async Task<Dictionary<string, string>> ProcessNewEntitiesAsync(List<ICreationHook> newEntities, string userId)
        {
            var entityCreationJobs = new Dictionary<string, string>();
            
            foreach (var entityHook in newEntities)
            {
                string entityType = entityHook.Type;
                string entityId = entityHook.Id;
                string entityName = entityHook.Name;
                string entityContext = entityHook.Context;
                
                if (string.IsNullOrEmpty(entityId) || string.IsNullOrEmpty(entityType))
                {
                    _loggingService.LogWarning($"Skipping entity creation with missing ID ('{entityId}') or Type ('{entityType}')");
                    continue;
                }
                
                if (await EntityExistsInWorldAsync(userId, entityType.ToLower(), entityId))
                {
                    _loggingService.LogInfo($"Skipping creation of {entityType} entity: {entityId} - already exists");
                    continue;
                }
                
                await RegisterEntityInWorldAsync(userId, entityHook, entityType.ToLower(), entityId, entityName);
                
                string jobId = ScheduleEntityCreation(entityHook, entityType, userId);
                if (!string.IsNullOrEmpty(jobId))
                {
                    entityCreationJobs.Add(entityId, jobId);
                }
            }
            
            return entityCreationJobs;
        }

        private async Task<bool> EntityExistsInWorldAsync(string userId, string entityTypeLower, string entityId)
        {
            var world = await _storageService.GetWorldAsync(userId);
            if (world == null) return false;
            
            return entityTypeLower switch
            {
                "npc" => world.Npcs?.Any(n => n.Id == entityId) ?? false,
                "location" => world.Locations?.Any(l => l.Id == entityId) ?? false,
                "quest" => world.Quests?.Any(q => q.Id == entityId) ?? false,
                _ => false
            };
        }

        private async Task RegisterEntityInWorldAsync(string userId, ICreationHook entityHook, string entityTypeLower, string entityId, string entityName)
        {
            string entityDisplayName = !string.IsNullOrEmpty(entityName) ? entityName : $"New {entityTypeLower}";
            
            await _storageService.AddEntityToWorldAsync(userId, entityId, entityDisplayName, entityTypeLower);
        }

        private string ScheduleEntityCreation(ICreationHook entityHook, string entityTypeUpper, string userId)
        {
            string entityId = entityHook.Id;
            string entityName = entityHook.Name;
            string context = entityHook.Context;
            
            if (string.IsNullOrEmpty(entityId) || string.IsNullOrEmpty(entityName) || string.IsNullOrEmpty(context))
            {
                 _loggingService.LogWarning($"Skipping scheduling for {entityTypeUpper} due to missing id/name/context.");
                 return string.Empty;
            }

            string jobId;
            string entityTypeLower = entityTypeUpper.ToLower();

            try
            {
                switch (entityTypeUpper)
                {
                    case "NPC":
                        if (entityHook is NpcCreationHook npcHook)
                        {
                            jobId = BackgroundJob.Enqueue(() =>
                                _serviceProvider.GetService<HangfireJobsService>().CreateNpcAsync(userId, npcHook.Id, npcHook.Name, npcHook.Context, npcHook.CurrentLocationId));
                            _loggingService.LogInfo($"Scheduled NPC creation job for {npcHook.Id}, job ID: {jobId}");
                        }
                        else { throw new InvalidCastException("Mismatched hook type for NPC"); }
                        break;

                    case "LOCATION":
                        if (entityHook is LocationCreationHook locHook)
                        {
                            jobId = BackgroundJob.Enqueue(() =>
                                _serviceProvider.GetService<HangfireJobsService>().CreateLocationAsync(userId, locHook.Id, locHook.Name, locHook.LocationType, locHook.Context));
                            _loggingService.LogInfo($"Scheduled location creation job for {locHook.Id}, job ID: {jobId}");
                        }
                         else { throw new InvalidCastException("Mismatched hook type for LOCATION"); }
                        break;

                    case "QUEST":
                         if (entityHook is QuestCreationHook questHook)
                        {
                            jobId = BackgroundJob.Enqueue(() =>
                                _serviceProvider.GetService<HangfireJobsService>().CreateQuestAsync(userId, questHook.Id, questHook.Name, questHook.Context));
                            _loggingService.LogInfo($"Scheduled quest creation job for {questHook.Id}, job ID: {jobId}");
                        }
                         else { throw new InvalidCastException("Mismatched hook type for QUEST"); }
                        break;

                    default:
                        _loggingService.LogWarning($"Unknown entity type for scheduling: {entityTypeUpper}");
                        return string.Empty;
                }

                _statusTrackingService.RegisterEntityCreationAsync(userId, entityId, entityTypeLower);

                return jobId;
            }
            catch (InvalidCastException castEx)
            {
                _loggingService.LogError($"Error scheduling entity creation for {entityId}: Hook type mismatch. {castEx.Message}");
                return string.Empty;
            }
            catch (Exception ex)
            {
                 _loggingService.LogError($"Error scheduling entity creation job for {entityTypeUpper} {entityId}: {ex.Message}");
                 return string.Empty;
            }
        }

        private async Task ProcessPartialUpdatesAsync(Dictionary<string, IUpdatePayload> partialUpdates, string userId, Dictionary<string, string> entityCreationJobs)
        {
            bool locationChanged = false;
            
            foreach (var kvp in partialUpdates)
            {
                var entityId = kvp.Key;
                var updateData = kvp.Value;
                
                if (updateData == null)
                {
                    _loggingService.LogWarning($"Invalid update data for entity {entityId}");
                    continue;
                }
                
                // Handle player special case
                if (updateData.Type.ToLower() == "player")
                {
                    if (updateData is PlayerUpdatePayload playerUpdate)
                    {
                        locationChanged = await ProcessPlayerUpdateAsync(userId, playerUpdate);
                    }
                    else
                    {
                        _loggingService.LogWarning($"Received non-PlayerUpdatePayload for player entity: {updateData.GetType().Name}");
                    }
                    continue;
                }
                
                await ProcessEntityUpdateAsync(userId, entityId, updateData, entityCreationJobs);
            }
            
            // Send location change notification after all updates are processed
            if (locationChanged)
            {
                await _notificationService.NotifyLocationChangedAsync(userId);
            }
        }

        private async Task<bool> ProcessPlayerUpdateAsync(string userId, PlayerUpdatePayload updateData)
        {
            _loggingService.LogInfo("Processing player update");
            
            // Check if there's a location change
            bool locationChanged = await CheckAndHandleLocationChangeAsync(userId, updateData);
            
            // Check if inventory is being updated
            bool inventoryChanged = updateData.Inventory != null && updateData.Inventory.Any();
            bool currencyChanged = updateData.Currencies != null && updateData.Currencies.Any();
            
            // Handle inventory updates if present
            if (inventoryChanged)
            {
                await ProcessPlayerInventoryUpdatesAsync(userId, updateData.Inventory);
            }
            
            // Handle currency updates if present
            if (currencyChanged)
            {
                await ProcessPlayerCurrencyUpdatesAsync(userId, updateData.Currencies);
            }
            
            // Convert the update data to JSON for storage update
            // Only send fields that are actually set (null fields are excluded)
            string updateJson = System.Text.Json.JsonSerializer.Serialize(updateData, _jsonSerializerOptions);
            
            // Only apply the update if there's something to update
            if (!string.IsNullOrEmpty(updateJson) && updateJson != "{}")
            {
                await UpdateEntityAsync(userId, "", "player", updateJson);
            }
            
            // Send notification if inventory or currency changed
            if (inventoryChanged || currencyChanged)
            {
                await _notificationService.NotifyInventoryChangedAsync(userId);
            }

            // Return whether the location changed instead of immediately sending notification
            return locationChanged;
        }

        private async Task<bool> CheckAndHandleLocationChangeAsync(string userId, PlayerUpdatePayload updateData)
        {
            var previousPlayer = await _storageService.GetPlayerAsync(userId);
            if (previousPlayer == null) return false; // Cannot check location if player doesn't exist
            
            string previousLocationId = previousPlayer.CurrentLocationId;
            string newLocationId = updateData.CurrentLocationId;
            
            if (!string.IsNullOrEmpty(newLocationId) && newLocationId != previousLocationId)
            {
                _loggingService.LogInfo($"Player location change detected: {previousLocationId} -> {newLocationId}");
                
                // When the player leaves a location, trigger conversation summarization
                await _serviceProvider.GetService<HangfireJobsService>().SummarizeConversationAsync(userId);
                _loggingService.LogInfo($"Triggered conversation summarization for user {userId} after location change");
                
                // Hide all NPCs in the previous location by setting VisibleToPlayer to false
                if (!string.IsNullOrEmpty(previousLocationId))
                {
                    int updatedCount = await _storageService.HideNpcsInLocationAsync(userId, previousLocationId);
                    _loggingService.LogInfo($"Updated visibility for {updatedCount} NPCs in previous location {previousLocationId}");
                }
                
                return true;
            }
            
            return false;
        }       

        private async Task ProcessEntityUpdateAsync(string userId, string entityId, IUpdatePayload updateData, Dictionary<string, string> entityCreationJobs)
        {
            // Get the entity type from the update data
            string entityType = updateData.Type?.ToLower();
            if (string.IsNullOrEmpty(entityType))
            {
                _loggingService.LogWarning($"Entity type missing for {entityId}");
                return;
            }
            
            // Enhanced logging
            _loggingService.LogInfo($"Processing entity update for {entityId} of type {entityType}");
            
            // Handle NPC special case
            if (entityType == "npc" && updateData is NpcUpdatePayload npcUpdate)
            {
                await ProcessNpcUpdateAsync(userId, entityId, npcUpdate);
                return;
            }
           
            // Wait for entity creation if needed
            if (entityCreationJobs.ContainsKey(entityId) && !await WaitForEntityCreationAsync(userId, entityId, entityType))
            {
                return; // Skip update if entity creation didn't complete
            }
            
            // Determine the collection based on entity type
            string collection = GetCollectionForEntityType(entityType);
            if (collection == null)
            {
                _loggingService.LogWarning($"Unknown entity type: {entityType} for entity {entityId}");
                return;
            }
            
            // Serialize the update data to JSON for storage
            // Explicitly use the concrete type for serialization
            string updateJson = "{}";
            try
            {
                // Serialize based on the actual runtime type
                if (updateData is NpcUpdatePayload npcPayload)
                {
                    updateJson = System.Text.Json.JsonSerializer.Serialize(npcPayload, _jsonSerializerOptions);
                }
                else if (updateData is LocationUpdatePayload locPayload)
                {
                    updateJson = System.Text.Json.JsonSerializer.Serialize(locPayload, _jsonSerializerOptions);
                }
                else if (updateData is PlayerUpdatePayload playerPayload)
                {
                    // Player updates are handled separately, but include for completeness
                    updateJson = System.Text.Json.JsonSerializer.Serialize(playerPayload, _jsonSerializerOptions);
                }
                 else if (updateData is WorldUpdatePayload worldPayload)
                {
                    updateJson = System.Text.Json.JsonSerializer.Serialize(worldPayload, _jsonSerializerOptions);
                }
                else
                {
                    // Fallback for any other IUpdatePayload types (if added later)
                     _loggingService.LogWarning($"Attempting to serialize unknown IUpdatePayload type: {updateData.GetType().Name}");
                    updateJson = System.Text.Json.JsonSerializer.Serialize(updateData, _jsonSerializerOptions);
                }
            }
            catch(Exception ex)
            {
                _loggingService.LogError($"Error during update JSON serialization for {entityId}: {ex.Message}");
                updateJson = "{}"; // Prevent applying faulty JSON
            }
            
            // Enhanced logging of the update JSON for debugging
            _loggingService.LogInfo($"Update JSON for {entityId}: {updateJson}");
            
            // Apply the update
            if (!string.IsNullOrEmpty(updateJson) && updateJson != "{}")
            {
                await UpdateEntityAsync(userId, collection, entityId, updateJson);                
            }
        }

        private async Task<bool> WaitForEntityCreationAsync(string userId, string entityId, string entityType)
        {
            _loggingService.LogInfo($"Waiting for {entityType} {entityId} to be created before applying update");
            try
            {
                // Instead of waiting directly for the job, we'll check the status
                // periodically (with a timeout) through the status tracking service
                var timeoutTask = Task.Delay(TimeSpan.FromMinutes(2));
                var checkInterval = TimeSpan.FromSeconds(2);
                var startTime = DateTime.UtcNow;
                bool isComplete = false;
                
                // Poll every few seconds to check if the entity creation is complete
                while (!isComplete && DateTime.UtcNow - startTime < TimeSpan.FromMinutes(2))
                {
                    await Task.Delay(checkInterval);
                    var status = await _statusTrackingService.GetEntityStatusAsync(userId, entityId);
                    isComplete = status != null && (status.Status == "complete" || status.Status == "error");
                }
                
                if (!isComplete)
                {
                    _loggingService.LogWarning($"Timed out waiting for {entityType} {entityId} to be created");
                    return false;
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error waiting for entity creation: {ex.Message}");
                return false;
            }
        }

        private string GetCollectionForEntityType(string entityType)
        {
            return entityType.ToLower() switch
            {
                "npc" => "npcs",
                "location" => "locations",
                "quest" => "quests",
                _ => null
            };
        }
        
        private async Task UpdateEntityAsync(string userId, string entityType, string entityId, string updateData)
        {
            var filePath = string.IsNullOrEmpty(entityType) 
                ? entityId 
                : $"{entityType}/{entityId}";
                
            try 
            {
                await _storageService.ApplyPartialUpdateAsync(userId, filePath, updateData);
                _loggingService.LogInfo($"Updated {entityType} entity: {entityId}");
            }
            catch (System.IO.FileNotFoundException)
            {
                _loggingService.LogWarning($"Cannot apply partial update to non-existent file: {filePath}");
            }
        }

        private async Task ProcessPlayerInventoryUpdatesAsync(string userId, List<InventoryUpdateItem> inventoryUpdates)
        {
            if (inventoryUpdates == null || !inventoryUpdates.Any())
            {
                return;
            }
            
            _loggingService.LogInfo($"Processing {inventoryUpdates.Count} inventory updates");
            
            // Keep track of indices to remove after processing
            var indicesToRemove = new List<int>();
            
            for (int i = 0; i < inventoryUpdates.Count; i++)
            {
                var itemUpdate = inventoryUpdates[i];
                string itemName = itemUpdate.Name;
                string itemDescription = itemUpdate.Description;
                UpdateAction action = itemUpdate.Action;
                int quantity = itemUpdate.Quantity;
                
                if (string.IsNullOrEmpty(itemName))
                {
                    _loggingService.LogWarning("Skipping inventory update with missing item name");
                    continue;
                }
                
                if (action == UpdateAction.Add)
                {
                    // Convert to an InventoryItem for storage
                    var item = new InventoryItem
                    {
                        Name = itemName,
                        Description = itemDescription ?? $"A {itemName}",
                        Quantity = quantity
                    };
                    
                    await _inventoryStorageService.AddItemToPlayerInventoryAsync(userId, item);
                    _loggingService.LogInfo($"Added {quantity}x {itemName} to player inventory");
                    
                    // Mark this item for removal to prevent double-processing
                    indicesToRemove.Add(i);
                }
                else if (action == UpdateAction.Remove)
                {
                    await _inventoryStorageService.RemoveItemFromPlayerInventoryAsync(userId, itemName, quantity);
                    _loggingService.LogInfo($"Removed {quantity}x {itemName} from player inventory");
                }
                else
                {
                    _loggingService.LogWarning($"Unknown inventory action: {action} for item {itemName}");
                }
            }
            
            // Remove processed items in reverse order to maintain correct indices
            for (int i = indicesToRemove.Count - 1; i >= 0; i--)
            {
                int indexToRemove = indicesToRemove[i];
                inventoryUpdates.RemoveAt(indexToRemove);
            }
        }

        private async Task ProcessPlayerCurrencyUpdatesAsync(string userId, List<CurrencyUpdateItem> currencyUpdates)
        {
            if (currencyUpdates == null || !currencyUpdates.Any())
            {
                return;
            }
            
            _loggingService.LogInfo($"Processing {currencyUpdates.Count} currency updates");
            
            // Keep track of indices to remove after processing
            var indicesToRemove = new List<int>();
            
            for (int i = 0; i < currencyUpdates.Count; i++)
            {
                var currencyUpdate = currencyUpdates[i];
                string currencyName = currencyUpdate.Name;
                UpdateAction action = currencyUpdate.Action;
                int amount = currencyUpdate.Amount;
                
                if (string.IsNullOrEmpty(currencyName))
                {
                    _loggingService.LogWarning("Skipping currency update with missing currency name");
                    continue;
                }
                
                if (action == UpdateAction.Add)
                {
                    await _inventoryStorageService.AddCurrencyAmountAsync(userId, currencyName, amount);
                    _loggingService.LogInfo($"Added {amount} {currencyName} to player");
                    
                    // Mark this item for removal to prevent double-processing
                    indicesToRemove.Add(i);
                }
                else if (action == UpdateAction.Remove)
                {
                    await _inventoryStorageService.RemoveCurrencyAmountAsync(userId, currencyName, amount);
                    _loggingService.LogInfo($"Removed {amount} {currencyName} from player");
                    
                    // Also mark Remove actions for removal to prevent them staying in the update
                    indicesToRemove.Add(i);
                }
                else
                {
                    _loggingService.LogWarning($"Unknown currency action: {action} for currency {currencyName}");
                }
            }
            
            // Remove processed items in reverse order to maintain correct indices
            for (int i = indicesToRemove.Count - 1; i >= 0; i--)
            {
                int indexToRemove = indicesToRemove[i];
                currencyUpdates.RemoveAt(indexToRemove);
            }
        }

        private async Task ProcessNpcUpdateAsync(string userId, string npcId, NpcUpdatePayload updateData)
        {
            _loggingService.LogInfo($"Processing NPC update for {npcId}");
            
            // Check if inventory is being updated
            bool inventoryChanged = updateData.Inventory != null && updateData.Inventory.Any();
            
            // Handle inventory updates if present
            if (inventoryChanged)
            {
                await ProcessNpcInventoryUpdatesAsync(userId, npcId, updateData.Inventory);
            }
            
            // Convert the update data to JSON for storage update
            string updateJson = System.Text.Json.JsonSerializer.Serialize(updateData, _jsonSerializerOptions);
            
            // Only apply the update if there's something to update
            if (!string.IsNullOrEmpty(updateJson) && updateJson != "{}")
            {
                await UpdateEntityAsync(userId, "npcs", npcId, updateJson);
            }
        }
        
        private async Task ProcessNpcInventoryUpdatesAsync(string userId, string npcId, List<InventoryUpdateItem> inventoryUpdates)
        {
            if (inventoryUpdates == null || !inventoryUpdates.Any())
            {
                return;
            }
            
            _loggingService.LogInfo($"Processing {inventoryUpdates.Count} inventory updates for NPC {npcId}");
            
            // Keep track of indices to remove after processing
            var indicesToRemove = new List<int>();
            
            // Get current NPC data to process inventory updates
            var npc = await _storageService.GetNpcAsync(userId, npcId);
            if (npc == null)
            {
                _loggingService.LogWarning($"Cannot process inventory for non-existent NPC: {npcId}");
                return;
            }
            
            // Initialize inventory if it doesn't exist
            if (npc.Inventory == null)
            {
                npc.Inventory = new List<InventoryItem>();
            }
            
            for (int i = 0; i < inventoryUpdates.Count; i++)
            {
                var itemUpdate = inventoryUpdates[i];
                string itemName = itemUpdate.Name;
                string itemDescription = itemUpdate.Description;
                UpdateAction action = itemUpdate.Action;
                int quantity = itemUpdate.Quantity;
                
                if (string.IsNullOrEmpty(itemName))
                {
                    _loggingService.LogWarning("Skipping NPC inventory update with missing item name");
                    continue;
                }
                
                // Process the update
                if (action == UpdateAction.Add)
                {
                    // Add item directly to NPC's inventory
                    var existingItem = npc.Inventory.FirstOrDefault(item => item.Name == itemName);
                    if (existingItem != null)
                    {
                        existingItem.Quantity += quantity;
                    }
                    else
                    {
                        npc.Inventory.Add(new InventoryItem
                        {
                            Name = itemName,
                            Description = itemDescription ?? $"A {itemName}",
                            Quantity = quantity
                        });
                    }
                    
                    _loggingService.LogInfo($"Added {quantity}x {itemName} to NPC {npcId} inventory");
                }
                else if (action == UpdateAction.Remove)
                {
                    // Remove item from NPC's inventory
                    var existingItem = npc.Inventory.FirstOrDefault(item => item.Name == itemName);
                    if (existingItem != null)
                    {
                        if (existingItem.Quantity <= quantity)
                        {
                            npc.Inventory.Remove(existingItem);
                        }
                        else
                        {
                            existingItem.Quantity -= quantity;
                        }
                        
                        _loggingService.LogInfo($"Removed {quantity}x {itemName} from NPC {npcId} inventory");
                    }
                    else
                    {
                        _loggingService.LogWarning($"Could not remove {itemName} from NPC {npcId} inventory - item not found");
                    }
                }
                else
                {
                    _loggingService.LogWarning($"Unknown inventory action: {action} for item {itemName}");
                    continue;
                }
                
                // Mark this item for removal from the update list
                indicesToRemove.Add(i);
            }
            
            // Save the updated NPC
            await _storageService.SaveAsync(userId, $"npcs/{npc.Id}", npc);
            
            // Remove processed items from the update list
            for (int i = indicesToRemove.Count - 1; i >= 0; i--)
            {
                int indexToRemove = indicesToRemove[i];
                inventoryUpdates.RemoveAt(indexToRemove);
            }
        }
    }
} 