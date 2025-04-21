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
    // Define the NpcEntriesPayload class for the new format
    public class NpcEntriesPayload : IUpdatePayload
    {
        public string Type { get; set; } = "npcEntries";
        public List<NpcUpdatePayload> Entries { get; set; } = new List<NpcUpdatePayload>();
    }

    // Define the LocationEntriesPayload class for the new format
    public class LocationEntriesPayload : IUpdatePayload
    {
        public string Type { get; set; } = "locationEntries";
        public List<LocationUpdatePayload> Entries { get; set; } = new List<LocationUpdatePayload>();
    }
    
    // Define RpgTagUpdateItem class for handling RPG tag updates
    public class RpgTagUpdateItem
    {
        public string Name { get; set; }
        public UpdateAction Action { get; set; }
    }
    
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
                PropertyNameCaseInsensitive = true,
            };
            _jsonSerializerOptions.Converters.Add(new LlmSafeIntConverter());
            _jsonSerializerOptions.Converters.Add(new PartialUpdatesConverter());
        }

        public async Task ProcessUpdatesAsync(List<ICreationHook> newEntities, PartialUpdates partialUpdates, string userId)
        {
            try
            {
                _loggingService.LogInfo($"Processing updates for user {userId}");

                if ((newEntities == null || !newEntities.Any()) && (partialUpdates == null))
                {
                    _loggingService.LogInfo("No updates to process - both newEntities and partialUpdates are null or empty");
                    return;
                }

                Dictionary<string, string> entityCreationJobs = null;
                if (newEntities != null && newEntities.Any())
                {
                    entityCreationJobs = await ProcessNewEntitiesAsync(newEntities, userId);
                }

                if (partialUpdates != null)
                {
                    if (entityCreationJobs != null && entityCreationJobs.Any())
                    {
                        // Get the ID of the last scheduled creation job to use for continuation
                        string lastJobId = entityCreationJobs.Values.LastOrDefault(jobId => !string.IsNullOrEmpty(jobId));

                        if (!string.IsNullOrEmpty(lastJobId))
                        {
                            _loggingService.LogInfo($"Scheduling partial updates to run after entity creation job {lastJobId} completes.");
                            // Schedule ProcessPartialUpdatesAsync to run only after the last entity creation job completes successfully.
                            BackgroundJob.ContinueJobWith(
                                lastJobId,
                                () => ProcessPartialUpdatesAsync(partialUpdates, userId), // No longer pass entityCreationJobs
                                JobContinuationOptions.OnlyOnSucceededState);
                        }
                        else
                        {
                            // This case might happen if all new entities already existed or failed scheduling. Run updates immediately.
                            _loggingService.LogInfo("No valid entity creation jobs were scheduled. Running partial updates immediately.");
                            await ProcessPartialUpdatesAsync(partialUpdates, userId); // No longer pass entityCreationJobs
                        }
                    }
                    else
                    {
                        // No new entities, process updates immediately or enqueue directly if desired.
                        _loggingService.LogInfo("No new entities to create. Processing partial updates immediately.");
                        await ProcessPartialUpdatesAsync(partialUpdates, userId); // No longer pass entityCreationJobs
                    }
                }
                // If partialUpdates is null but newEntities exist, the creation jobs are scheduled, and we are done here.
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error in ProcessUpdatesAsync: {ex.Message}\nStackTrace: {ex.StackTrace}");
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
                                _serviceProvider.GetService<HangfireJobsService>().CreateNpcAsync(userId, npcHook.Id, npcHook.Name, npcHook.Context, npcHook.CurrentLocationId, false, null));
                            _loggingService.LogInfo($"Scheduled NPC creation job for {npcHook.Id}, job ID: {jobId}");
                        }
                        else { throw new InvalidCastException("Mismatched hook type for NPC"); }
                        break;

                    case "LOCATION":
                        if (entityHook is LocationCreationHook locHook)
                        {
                            jobId = BackgroundJob.Enqueue(() =>
                                _serviceProvider.GetService<HangfireJobsService>().CreateLocationAsync(userId, locHook.Id, locHook.Name, locHook.LocationType, locHook.Context, null, false, null));
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

        public async Task ProcessPartialUpdatesAsync(PartialUpdates partialUpdates, string userId)
        {
            _loggingService.LogInfo($"Starting to process partial updates for user {userId}");
            
            try {
                bool locationChanged = false;
                
                // Process player update if present
                if (partialUpdates.Player != null)
                {
                    _loggingService.LogInfo($"Processing player updates for user {userId}");
                    locationChanged = await ProcessPlayerUpdateAsync(userId, partialUpdates.Player);
                }
                
                // Process world update if present
                if (partialUpdates.World != null)
                {
                    _loggingService.LogInfo($"Processing world updates for user {userId}");
                    string updateJson = System.Text.Json.JsonSerializer.Serialize(partialUpdates.World, _jsonSerializerOptions);
                    await UpdateEntityAsync(userId, "", "world", updateJson);
                }
                
                // Process NPC entries
                if (partialUpdates.NpcEntries != null && partialUpdates.NpcEntries.Count > 0)
                {
                    _loggingService.LogInfo($"Processing {partialUpdates.NpcEntries.Count} NPC entries for user {userId}");
                    foreach (var npcUpdate in partialUpdates.NpcEntries)
                    {
                        if (string.IsNullOrEmpty(npcUpdate.Id))
                        {
                            _loggingService.LogWarning("Skipping NPC update with missing ID");
                            continue;
                        }
                        
                        _loggingService.LogInfo($"Processing NPC update for {npcUpdate.Id}");
                        
                        // Process normally
                        await ProcessNpcUpdateAsync(userId, npcUpdate.Id, npcUpdate);
                    }
                }
                
                // Process location entries
                if (partialUpdates.LocationEntries != null && partialUpdates.LocationEntries.Count > 0)
                {
                    _loggingService.LogInfo($"Processing {partialUpdates.LocationEntries.Count} location entries for user {userId}");
                    foreach (var locationUpdate in partialUpdates.LocationEntries)
                    {
                        if (string.IsNullOrEmpty(locationUpdate.Id))
                        {
                            _loggingService.LogWarning("Skipping location update with missing ID");
                            continue;
                        }
                        
                        _loggingService.LogInfo($"Processing location update for {locationUpdate.Id}");
                        await ProcessEntityUpdateAsync(userId, locationUpdate.Id, locationUpdate);
                    }
                }
                
                // Send location change notification after all updates are processed
                if (locationChanged)
                {
                    _loggingService.LogInfo($"Location changed for user {userId}, sending notification");
                    await _notificationService.NotifyLocationChangedAsync(userId);
                }
                
                _loggingService.LogInfo($"Completed processing all partial updates for user {userId}");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error processing partial updates for user {userId}: {ex.Message}");
                _loggingService.LogError($"Exception stack trace: {ex.StackTrace}");
                throw; // Re-throw to allow calling code/Hangfire to handle
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
            bool statusEffectsChanged = updateData.StatusEffects != null && updateData.StatusEffects.Any();
            bool rpgTagsChanged = updateData.RpgTags != null && updateData.RpgTags.Any();
            
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

            // Handle status effects updates if present
            if (statusEffectsChanged)
            {
                await ProcessPlayerStatusEffectsAsync(userId, updateData.StatusEffects);
            }
            
            // Handle RPG tags updates if present
            if (rpgTagsChanged)
            {
                await ProcessPlayerRpgTagsAsync(userId, updateData.RpgTags);
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
                
                return true;
            }
            
            return false;
        }       

        private async Task ProcessEntityUpdateAsync(string userId, string entityId, IUpdatePayload updateData)
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
            
            // Handle Location with NPCs array special case
            if (entityType == "location" && updateData is LocationUpdatePayload locationUpdate && locationUpdate.Npcs != null && locationUpdate.Npcs.Any())
            {                
                // If there are other properties to update, create a copy without the Npcs property
                var locationUpdateWithoutNpcs = new LocationUpdatePayload
                {
                    Id = locationUpdate.Id,
                    KnownToPlayer = locationUpdate.KnownToPlayer,
                    ParentLocation = locationUpdate.ParentLocation
                };
                
                // Check if there are actually other properties to update besides Npcs
                if (locationUpdateWithoutNpcs.KnownToPlayer != null || !string.IsNullOrEmpty(locationUpdateWithoutNpcs.ParentLocation))
                {
                     _loggingService.LogInfo($"Processing remaining location properties for {entityId}");
                     updateData = locationUpdateWithoutNpcs;
                }
                else
                {
                     _loggingService.LogInfo($"Only NPC updates found for location {entityId}. Skipping further processing for this update object.");
                     return; // No other properties to update
                }
            }
            
            // If no pending creation or continuation not scheduled, proceed with normal update
            // Determine the collection based on entity type
            string collection = GetCollectionForEntityType(entityType);
            if (collection == null)
            {
                _loggingService.LogWarning($"Unknown entity type: {entityType} for entity {entityId}");
                return;
            }
            
            // Serialize the update data to JSON for storage
            string updateJson = SerializeUpdateData(updateData);
            
            // Don't apply empty updates
            if (string.IsNullOrWhiteSpace(updateJson) || updateJson == "{}" )
            {
                 _loggingService.LogInfo($"Skipping update for {entityType} {entityId} as serialized data is empty.");
                 return;
            }
            
            // Apply the update
            await UpdateEntityAsync(userId, collection, entityId, updateJson);
        }
        
        /// <summary>
        /// Serializes update data to JSON based on its runtime type
        /// </summary>
        private string SerializeUpdateData(IUpdatePayload updateData)
        {
            try
            {
                // Serialize based on the actual runtime type
                if (updateData is NpcUpdatePayload npcPayload)
                {
                    return System.Text.Json.JsonSerializer.Serialize(npcPayload, _jsonSerializerOptions);
                }
                else if (updateData is LocationUpdatePayload locationPayload)
                {
                    return System.Text.Json.JsonSerializer.Serialize(locationPayload, _jsonSerializerOptions);
                }
                else if (updateData is WorldUpdatePayload worldPayload)
                {
                    return System.Text.Json.JsonSerializer.Serialize(worldPayload, _jsonSerializerOptions);
                }
                else if (updateData is PlayerUpdatePayload playerPayload)
                {
                    return System.Text.Json.JsonSerializer.Serialize(playerPayload, _jsonSerializerOptions);
                }
                else
                {
                    // Generic serialization
                    return System.Text.Json.JsonSerializer.Serialize(updateData, _jsonSerializerOptions);
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error serializing update data: {ex.Message}");
                return "{}";
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
                    // Mark this item for removal to prevent double-processing
                    indicesToRemove.Add(i);
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

        private async Task ProcessPlayerStatusEffectsAsync(string userId, List<StatusEffectUpdateItem> statusEffectUpdates)
        {
            if (statusEffectUpdates == null || !statusEffectUpdates.Any())
            {
                return;
            }
            
            _loggingService.LogInfo($"Processing {statusEffectUpdates.Count} status effect updates");
            
            // Get current player to update status effects directly
            var player = await _storageService.GetPlayerAsync(userId);
            if (player == null)
            {
                _loggingService.LogError($"Failed to load player for status effect updates for user {userId}");
                return;
            }
            
            // Initialize the list if it's null
            if (player.StatusEffects == null)
            {
                player.StatusEffects = new List<string>();
            }
            
            // Keep track of indices to remove after processing
            var indicesToRemove = new List<int>();
            
            for (int i = 0; i < statusEffectUpdates.Count; i++)
            {
                var effectUpdate = statusEffectUpdates[i];
                string effectName = effectUpdate.Name;
                UpdateAction action = effectUpdate.Action;
                
                if (string.IsNullOrEmpty(effectName))
                {
                    _loggingService.LogWarning("Skipping status effect update with missing effect name");
                    continue;
                }
                
                if (action == UpdateAction.Add)
                {
                    // Don't add duplicates
                    if (!player.StatusEffects.Contains(effectName))
                    {
                        player.StatusEffects.Add(effectName);
                        _loggingService.LogInfo($"Added status effect '{effectName}' to player");
                        
                        // Send notification about the new status effect
                        await _notificationService.NotifyGenericAsync(userId, $"Added status effect: {effectName}");
                    }
                    // Mark this item for removal to prevent double-processing
                    indicesToRemove.Add(i);
                }
                else if (action == UpdateAction.Remove)
                {
                    if (player.StatusEffects.Contains(effectName))
                    {
                        player.StatusEffects.Remove(effectName);
                        _loggingService.LogInfo($"Removed status effect '{effectName}' from player");
                        
                        // Send notification about the removed status effect
                        await _notificationService.NotifyGenericAsync(userId, $"Removed status effect: {effectName}");
                    }
                    // Mark this item for removal to prevent double-processing
                    indicesToRemove.Add(i);
                }
                else
                {
                    _loggingService.LogWarning($"Unknown status effect action: {action} for effect {effectName}");
                }
            }
            
            // Save the updated player with modified status effects
            await _storageService.SaveAsync(userId, "player", player);
            
            // Remove processed items in reverse order to maintain correct indices
            for (int i = indicesToRemove.Count - 1; i >= 0; i--)
            {
                int indexToRemove = indicesToRemove[i];
                statusEffectUpdates.RemoveAt(indexToRemove);
            }
        }

        private async Task ProcessPlayerRpgTagsAsync(string userId, List<Models.RpgTagUpdateItem> rpgTagUpdates)
        {
            if (rpgTagUpdates == null || !rpgTagUpdates.Any())
            {
                return;
            }
            
            _loggingService.LogInfo($"Processing {rpgTagUpdates.Count} RPG tag updates");
            
            // Get current player to update RPG tags directly
            var player = await _storageService.GetPlayerAsync(userId);
            if (player == null)
            {
                _loggingService.LogError($"Failed to load player for RPG tag updates for user {userId}");
                return;
            }
            
            // Initialize the list if it's null
            if (player.RpgTags == null)
            {
                player.RpgTags = new List<Models.RpgTag>();
            }
            
            // Keep track of indices to remove after processing
            var indicesToRemove = new List<int>();
            
            for (int i = 0; i < rpgTagUpdates.Count; i++)
            {
                var tagUpdate = rpgTagUpdates[i];
                string tagName = tagUpdate.Name;
                UpdateAction action = tagUpdate.Action;
                
                if (string.IsNullOrEmpty(tagName))
                {
                    _loggingService.LogWarning("Skipping RPG tag update with missing tag name");
                    continue;
                }
                
                if (action == UpdateAction.Add)
                {
                    // Don't add duplicates
                    if (!player.RpgTags.Any(tag => tag.Name == tagName))
                    {
                        player.RpgTags.Add(new Models.RpgTag 
                        { 
                            Name = tagName,
                            Description = tagUpdate.Description ?? $"Tag: {tagName}"
                        });
                        _loggingService.LogInfo($"Added RPG tag '{tagName}' to player");
                    }
                    // Mark this item for removal to prevent double-processing
                    indicesToRemove.Add(i);
                }
                else if (action == UpdateAction.Remove)
                {
                    var existingTag = player.RpgTags.FirstOrDefault(tag => tag.Name == tagName);
                    if (existingTag != null)
                    {
                        player.RpgTags.Remove(existingTag);
                        _loggingService.LogInfo($"Removed RPG tag '{tagName}' from player");
                    }
                    // Mark this item for removal to prevent double-processing
                    indicesToRemove.Add(i);
                }
                else
                {
                    _loggingService.LogWarning($"Unknown RPG tag action: {action} for tag {tagName}");
                }
            }
            
            // Save the updated player with modified RPG tags
            await _storageService.SaveAsync(userId, "player", player);
            
            // Remove processed items in reverse order to maintain correct indices
            for (int i = indicesToRemove.Count - 1; i >= 0; i--)
            {
                int indexToRemove = indicesToRemove[i];
                rpgTagUpdates.RemoveAt(indexToRemove);
            }
        }

        private async Task ProcessNpcUpdateAsync(string userId, string npcId, NpcUpdatePayload updateData)
        {
            _loggingService.LogInfo($"Processing NPC update for {npcId}");
            
            // Handle inventory updates first if present
            if (updateData.Inventory != null && updateData.Inventory.Any())
            {
                await ProcessNpcInventoryUpdatesAsync(userId, npcId, updateData.Inventory);
            }
            
            // Convert the update data to JSON for storage update
            string updateJson = System.Text.Json.JsonSerializer.Serialize(updateData, _jsonSerializerOptions);
            
            // Only apply the update if there's something to update beyond inventory
             if (!string.IsNullOrEmpty(updateJson) && updateJson != "{}")
            {
                // Check if the only thing serialized was an empty inventory list we already processed
                var tempDeserialize = System.Text.Json.JsonSerializer.Deserialize<NpcUpdatePayload>(updateJson, _jsonSerializerOptions);
                if (tempDeserialize != null && (tempDeserialize.VisibleToPlayer != null || tempDeserialize.VisualDescription != null)) // Add other NPC fields here if needed
                {
                     await UpdateEntityAsync(userId, "npcs", npcId, updateJson);
                }
                 else if (updateData.Inventory == null || !updateData.Inventory.Any()) // If inventory was null/empty AND nothing else was set
                 {
                      _loggingService.LogInfo($"Skipping empty NPC update for {npcId}");
                 }
                 else // Only inventory was updated, which we handled above.
                 {
                      _loggingService.LogInfo($"Only inventory updates found for NPC {npcId}. Already processed.");
                 }
            }
             else if (updateData.Inventory == null || !updateData.Inventory.Any()) // If inventory was null/empty AND updateJson was empty/{}
             {
                 _loggingService.LogInfo($"Skipping empty NPC update for {npcId}");
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
   