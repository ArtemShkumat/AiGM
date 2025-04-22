using System;
using System.Threading.Tasks;
using AiGMBackEnd.Models;
using AiGMBackEnd.Models.Prompts;
using AiGMBackEnd.Services.Processors;
using Newtonsoft.Json.Linq;
using Hangfire;
using Hangfire.Server;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace AiGMBackEnd.Services
{
    public class HangfireJobsService
    {
        // In-memory dictionary to store job results
        private static readonly ConcurrentDictionary<string, string> _jobResults = new ConcurrentDictionary<string, string>();
        
        private readonly LoggingService _loggingService;
        private readonly PromptService _promptService;
        private readonly AiService _aiService;
        private readonly ResponseProcessingService _responseProcessingService;
        private readonly IStatusTrackingService _statusTrackingService;
        private readonly StorageService _storageService;
        private readonly INPCProcessor _npcProcessor;
        private readonly ILocationProcessor _locationProcessor;
        private readonly IQuestProcessor _questProcessor;
        private readonly GameNotificationService _gameNotificationService;

        public HangfireJobsService(
            LoggingService loggingService,
            PromptService promptService,
            AiService aiService,
            ResponseProcessingService responseProcessingService,
            IStatusTrackingService statusTrackingService,
            StorageService storageService,
            INPCProcessor npcProcessor,
            ILocationProcessor locationProcessor,
            IQuestProcessor questProcessor,
            GameNotificationService gameNotificationService)
        {
            _loggingService = loggingService;
            _promptService = promptService;
            _aiService = aiService;
            _responseProcessingService = responseProcessingService;
            _statusTrackingService = statusTrackingService;
            _storageService = storageService;
            _npcProcessor = npcProcessor;
            _locationProcessor = locationProcessor;
            _questProcessor = questProcessor;
            _gameNotificationService = gameNotificationService;
        }

        /// <summary>
        /// Creates an entity based on the prompt type and context
        /// </summary>
        public async Task CreateEntityAsync(string userId, string entityId, string entityType, PromptRequest request)
        {
            try
            {
                _loggingService.LogInfo($"Starting entity creation job for {entityType} {entityId}");
                
                // Check if this is for a starting scenario
                bool isStartingScenario = request.IsStartingScenario;
                
                string jobContext = isStartingScenario ? "starting scenario template" : "user game";
                _loggingService.LogInfo($"Entity creation is for {jobContext}");
                
                // Register that we're creating this entity
                await _statusTrackingService.RegisterEntityCreationAsync(userId, entityId, entityType);
                
                // 1. Build prompt
                var prompt = await _promptService.BuildPromptAsync(request);
                _loggingService.LogInfo($"Built prompt for entity creation {entityType} {entityId}");
                
                // 2. Call LLM
                var llmResponse = await _aiService.GetCompletionAsync(prompt);
                _loggingService.LogInfo($"LLM response received for {entityType} {entityId}, length: {llmResponse?.Length ?? 0}");
                
                // 3. Process the response, passing scenario context
                var processedResult = await _responseProcessingService.HandleCreateResponseAsync(
                    llmResponse, 
                    request.PromptType, 
                    userId,
                    request.IsStartingScenario, // Pass the flag
                    request.ScenarioId        // Pass the ID
                );
                
                // 4. Sync world with entities
                await _storageService.SyncWorldWithEntitiesAsync(userId);
                
                // 5. Update entity status to complete
                await _statusTrackingService.UpdateEntityStatusAsync(userId, entityId, "complete");
                
                _loggingService.LogInfo($"Successfully created {entityType} {entityId}");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error creating {entityType} {entityId}: {ex.Message}");
                await _statusTrackingService.UpdateEntityStatusAsync(userId, entityId, "error", ex.Message);
                throw; // Rethrow so Hangfire can mark the job as failed
            }
        }
        
        /// <summary>
        /// Creates an NPC entity 
        /// </summary>
        public async Task CreateNpcAsync(string userId, string npcId, string npcName, string context, string currentLocationId, bool isStartingScenario = false, string scenarioId = null)
        {
            var request = new PromptRequest 
            { 
                PromptType = PromptType.CreateNPC,
                UserId = userId,
                Context = context,
                NpcLocation = currentLocationId,
                NpcName = npcName,
                NpcId = npcId,
                ScenarioId = scenarioId,
                IsStartingScenario = isStartingScenario
            };
            
            await CreateEntityAsync(userId, npcId, "npc", request);
        }
        
        /// <summary>
        /// Creates an NPC entity with additional metadata
        /// </summary>
        public async Task CreateNpcWithMetadataAsync(string userId, string npcId, string npcName, string context, string currentLocationId, bool isStartingScenario, Dictionary<string, string> additionalMetadata)
        {
            var request = new PromptRequest 
            { 
                PromptType = PromptType.CreateNPC,
                UserId = userId,
                Context = context,
                NpcLocation = currentLocationId,
                NpcName = npcName,
                NpcId = npcId,
                IsStartingScenario = isStartingScenario
            };
            
            // Extract scenarioId from additionalMetadata if present
            if (additionalMetadata != null && additionalMetadata.TryGetValue("scenarioId", out string scenarioId))
            {
                request.ScenarioId = scenarioId;
            }
            
            await CreateEntityAsync(userId, npcId, "npc", request);
        }
        
        /// <summary>
        /// Creates a location entity
        /// </summary>
        public async Task CreateLocationAsync(string userId, string locationId, string locationName, string locationType, string context, string parentLocationId = null, bool isStartingScenario = false, string scenarioId = null)
        {
            var request = new PromptRequest 
            { 
                PromptType = PromptType.CreateLocation,
                UserId = userId,
                LocationId = locationId,
                LocationName = locationName,
                LocationType = locationType,
                Context = context,
                ParentLocationId = parentLocationId,
                ScenarioId = scenarioId,
                IsStartingScenario = isStartingScenario
            };
            
            await CreateEntityAsync(userId, locationId, "location", request);
        }

        /// <summary>
        /// Creates a location entity with additional metadata
        /// </summary>
        public async Task CreateLocationWithMetadataAsync(string userId, string locationId, string locationName, string locationType, string context, string parentLocationId = null, bool isStartingScenario = false, Dictionary<string, string> additionalMetadata = null)
        {
            var request = new PromptRequest 
            { 
                PromptType = PromptType.CreateLocation,
                UserId = userId,
                LocationId = locationId,
                LocationName = locationName,
                LocationType = locationType,
                Context = context,
                ParentLocationId = parentLocationId,
                IsStartingScenario = isStartingScenario
            };
            
            // Extract scenarioId from additionalMetadata if present
            if (additionalMetadata != null && additionalMetadata.TryGetValue("scenarioId", out string scenarioId))
            {
                request.ScenarioId = scenarioId;
            }
            
            await CreateEntityAsync(userId, locationId, "location", request);
        }
        
        /// <summary>
        /// Creates a quest entity
        /// </summary>
        public async Task CreateQuestAsync(string userId, string questId, string questName, string context)
        {
            var request = new PromptRequest 
            { 
                PromptType = PromptType.CreateQuest,
                UserId = userId,
                QuestId = questId,
                QuestName = questName,
                Context = context
            };
            
            await CreateEntityAsync(userId, questId, "quest", request);
        }
        
        /// <summary>
        /// Creates a scenario based on a prompt
        /// </summary>
        public async Task CreateScenarioAsync(string userId, string scenarioId, string scenarioPrompt, bool isStartingScenario = false)
        {
            var request = new PromptRequest 
            { 
                PromptType = PromptType.CreateScenario,
                UserId = userId,
                UserInput = scenarioPrompt,
                ScenarioId = scenarioId,
                IsStartingScenario = isStartingScenario
            };
            
            await CreateEntityAsync(userId, scenarioId, "scenario", request);
        }
        
        /// <summary>
        /// Processes a conversation to generate a summary. Used when a player leaves a location.
        /// </summary>
        public async Task SummarizeConversationAsync(string userId)
        {
            try
            {
                _loggingService.LogInfo($"Starting conversation summarization for user {userId}");
                
                // Create the prompt request
                var request = new PromptRequest
                {
                    PromptType = PromptType.Summarize,
                    UserId = userId
                };
                
                // Process the summarization in the background
                var jobId = BackgroundJob.Enqueue(() => ProcessSummarizationJobAsync(request));
                
                _loggingService.LogInfo($"Scheduled summarization job for user {userId}, job ID: {jobId}");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error scheduling summarization job: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Background job to process summarization (both general and combat)
        /// </summary>
        public async Task ProcessSummarizationJobAsync(PromptRequest request)
        {
            try
            {
                _loggingService.LogInfo($"Processing summarization job ({request.PromptType}) for user {request.UserId}");
                
                // 1. Build the summarization prompt (builder handles context based on type)
                var prompt = await _promptService.BuildPromptAsync(request);
                _loggingService.LogInfo($"Built {request.PromptType} prompt for user {request.UserId}");
                
                // 2. Call LLM to generate summary
                var llmResponse = await _aiService.GetCompletionAsync(prompt);
                _loggingService.LogInfo($"LLM response received for {request.PromptType}, length: {llmResponse?.Length ?? 0}");
                
                // 3. Process the summary response using the appropriate processor method
                if (request.PromptType == PromptType.SummarizeCombat)
                {
                    // Extract victory status from the request context
                    bool playerVictory = false;
                    if (!string.IsNullOrEmpty(request.Context))
                    {
                        bool.TryParse(request.Context, out playerVictory);
                    }

                    // Pass victory status to the processing method
                    await _responseProcessingService.ProcessCombatSummaryAsync(llmResponse, request.UserId, playerVictory);
                    _loggingService.LogInfo($"Successfully processed combat summary for user {request.UserId}");
                    
                    // Clean up combat state AFTER summary is processed
                    await _storageService.DeleteCombatStateAsync(request.UserId);
                    _loggingService.LogInfo($"Deleted combat state for user {request.UserId} after summarization.");
                }
                else // Assume PromptType.Summarize
                {
                    // Need a corresponding method in ResponseProcessingService
                    await _responseProcessingService.ProcessGeneralSummaryAsync(llmResponse, request.UserId);
                    _loggingService.LogInfo($"Successfully processed general summary for user {request.UserId}");
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error processing summarization job ({request.PromptType}) for {request.UserId}: {ex.Message}");
                // Should we delete combat state on error? Maybe not, allow retry?
                throw;
            }
        }
        
        /// <summary>
        /// Ensures an enemy stat block exists and then initiates combat.
        /// Uses Hangfire continuations.
        /// </summary>
        public async Task EnsureEnemyStatBlockAndInitiateCombatAsync(string userId, string enemyId, string initialCombatText)
        {
            _loggingService.LogInfo($"Ensuring stat block exists for enemy {enemyId} before initiating combat for user {userId}");
            
            string creationJobId = null;
            bool exists = await _storageService.CheckIfStatBlockExistsAsync(userId, enemyId);
            
            if (!exists)
            {
                _loggingService.LogInfo($"Stat block for enemy {enemyId} does not exist. Enqueuing creation job.");
                // TODO: Need a way to get the NPC/Enemy details (name, context) to pass to CreateEnemyStatBlockAsync
                // This likely requires loading the NPC model based on enemyId
                var npc = await _storageService.GetNpcAsync(userId, enemyId);
                if (npc == null)
                {
                    _loggingService.LogError($"Cannot create stat block. NPC with ID {enemyId} not found for user {userId}.");
                    // How to handle this? Maybe notify user combat can't start?
                    // For now, just log and don't proceed.
                    return; 
                }
                
                // Enqueue the creation job
                creationJobId = BackgroundJob.Enqueue(() => CreateEnemyStatBlockAsync(userId, enemyId, npc.Name, npc.Backstory)); // Assuming context is backstory for now
                _loggingService.LogInfo($"Enqueued stat block creation job {creationJobId} for enemy {enemyId}");
            }
            else
            {
                _loggingService.LogInfo($"Stat block for enemy {enemyId} already exists.");
            }

            // Schedule the InitiateCombatAsync job to run AFTER the creation job (if one was needed)
            if (!string.IsNullOrEmpty(creationJobId))
            {
                BackgroundJob.ContinueJobWith(creationJobId, 
                    () => InitiateCombatAsync(userId, enemyId, initialCombatText), 
                    JobContinuationOptions.OnAnyFinishedState); // Run even if creation fails? Decide policy.
                _loggingService.LogInfo($"Scheduled combat initiation to continue after job {creationJobId}.");
            }
            else
            {
                // Stat block already existed, schedule combat initiation immediately
                BackgroundJob.Enqueue(() => InitiateCombatAsync(userId, enemyId, initialCombatText));
                _loggingService.LogInfo($"Scheduling combat initiation immediately for enemy {enemyId}.");
            }
        }

        /// <summary>
        /// Creates an Enemy Stat Block entity.
        /// </summary>
        public async Task CreateEnemyStatBlockAsync(string userId, string enemyId, string enemyName, string context)
        {
            var request = new PromptRequest
            {
                PromptType = PromptType.CreateEnemyStatBlock,
                UserId = userId,
                NpcId = enemyId, // Using NpcId field to pass the enemy ID
                NpcName = enemyName,
                Context = context // Pass context for LLM
            };
            
            // Use the existing CreateEntityAsync but specify the type as "enemy"
            await CreateEntityAsync(userId, enemyId, "enemy", request);
        }

        /// <summary>
        /// Initiates combat by creating the CombatState and notifying the frontend.
        /// This is intended to be run as a Hangfire job, often as a continuation.
        /// </summary>
        public async Task InitiateCombatAsync(string userId, string enemyId, string initialCombatText)
        {
            try
            {
                _loggingService.LogInfo($"Initiating combat for user {userId} against enemy {enemyId}.");

                // Double-check stat block exists now (might have failed creation)
                var enemyStatBlock = await _storageService.LoadEnemyStatBlockAsync(userId, enemyId);
                if (enemyStatBlock == null)
                {
                    _loggingService.LogError($"Failed to initiate combat: Stat block for enemy {enemyId} still not found after check.");
                    // Notify user? Send specific error SignalR message?
                    // For now, just log and exit.
                    return;
                }

                // Create and save the CombatState
                var combatState = new CombatState
                {
                    CombatId = Guid.NewGuid().ToString(),
                    UserId = userId,
                    EnemyStatBlockId = enemyId,
                    CurrentEnemySuccesses = 0,
                    PlayerConditions = new List<string>(),
                    CombatLog = new List<string> { initialCombatText }, // Use the text from the triggering response
                    IsActive = true
                };
                await _storageService.SaveCombatStateAsync(userId, combatState);

                // Create the CombatStartInfo DTO
                var combatStartInfo = new CombatStartInfo
                {
                    CombatId = combatState.CombatId,
                    EnemyId = enemyStatBlock.Id,
                    EnemyName = enemyStatBlock.Name,
                    EnemyDescription = enemyStatBlock.Description,
                    EnemyLevel = enemyStatBlock.Level,
                    SuccessesRequired = enemyStatBlock.SuccessesRequired,
                    PlayerConditions = combatState.PlayerConditions
                };

                // Notify the frontend
                await _gameNotificationService.NotifyCombatStartedAsync(userId, combatStartInfo);
                _loggingService.LogInfo($"Successfully initiated combat {combatState.CombatId} and notified user {userId}.");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error during InitiateCombatAsync for user {userId}, enemy {enemyId}: {ex.Message}\nStackTrace: {ex.StackTrace}");
                // Consider sending an error notification to the user via SignalR
                throw; // Rethrow for Hangfire failure tracking
            }
        }

        /// <summary>
        /// Processes a normal user input (DM or NPC response). Accepts PerformContext for Job ID access.
        /// </summary>
        public async Task<string> ProcessUserInputAsync(PromptRequest request, PerformContext context)
        {
            try
            {
                var jobId = context.BackgroundJob.Id; // Get the current Hangfire Job ID
                _loggingService.LogInfo($"Processing user input for {request.PromptType}, user {request.UserId}, Job ID: {jobId}");
                
                // 1. Build prompt
                var prompt = await _promptService.BuildPromptAsync(request);
                _loggingService.LogInfo($"Built prompt for user input (Job ID: {jobId})");
                
                // 2. Call LLM
                var llmResponse = await _aiService.GetCompletionAsync(prompt);
                _loggingService.LogInfo($"LLM response received, length: {llmResponse?.Length ?? 0} (Job ID: {jobId})");
               
                
                // 3. Process the response
                ProcessedResult processedResult;
                if (request.PromptType == PromptType.DM || request.PromptType == PromptType.NPC || request.PromptType == PromptType.Combat)
                {
                    _loggingService.LogInfo($"Processing interactive response ({request.PromptType}) (Job ID: {jobId})");
                    processedResult = await _responseProcessingService.HandleResponseAsync(llmResponse, request.PromptType, request.UserId, request.NpcId);
                }
                else // Assume it's a creation prompt
                {
                     _loggingService.LogInfo($"Processing creation response ({request.PromptType}) (Job ID: {jobId})");
                    processedResult = await _responseProcessingService.HandleCreateResponseAsync(
                        llmResponse, 
                        request.PromptType, 
                        request.UserId,
                        request.IsStartingScenario, // Pass the flag
                        request.ScenarioId        // Pass the ID
                    );
                }
                
                // 4. Sync world with entities if needed (HandleResponseAsync doesn't do this anymore for combat trigger)
                if (!processedResult.CombatInitiated && !processedResult.CombatPending) 
                {
                    await _storageService.SyncWorldWithEntitiesAsync(request.UserId);
                }
                
                // Store the result in our dictionary using the Job ID as the key
                string key = $"job_result:{jobId}"; // Key is now based on Job ID
                _jobResults[key] = JsonConvert.SerializeObject(processedResult); // Store the whole result object
                
                _loggingService.LogInfo($"Successfully processed user input for {request.PromptType}, stored result with key {key} (Job ID: {jobId})");
                return processedResult.UserFacingText; // Return ONLY the user-facing text
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error processing user input: {ex.Message}");
                var errorResult = new ProcessedResult { Success = false, ErrorMessage = ex.Message, UserFacingText = "An error occurred processing your request." };
                string key = $"job_result:{context.BackgroundJob.Id}"; 
                _jobResults[key] = JsonConvert.SerializeObject(errorResult); // Store error details
                return errorResult.UserFacingText; // Return ONLY the user-facing error text
            }
        }
        
        /// <summary>
        /// Gets the processed result for a job. This is used when we need to retrieve
        /// the result from a completed Hangfire job.
        /// </summary>
        public async Task<string> GetProcessedResultAsync(string jobId, string userId)
        {
             string key = $"job_result:{jobId}";
            
            if (_jobResults.TryGetValue(key, out string resultJson))
            {
                // Attempt to deserialize. If it fails, return the raw JSON.
                try
                {
                    var result = JsonConvert.DeserializeObject<ProcessedResult>(resultJson);
                    _loggingService.LogInfo($"Retrieved stored result for Job ID: {jobId}");
                    
                    // Check if there are pending entities after retrieving the result
                    bool hasPendingEntities = await _statusTrackingService.HasPendingEntitiesAsync(userId);
                    
                    if(hasPendingEntities)
                    {
                        _loggingService.LogInfo($"User {userId} has pending entity creations after job {jobId}.");
                        // Optionally modify result or add information here if needed
                    }
                    
                    // Remove the result once retrieved to prevent memory leak
                    _jobResults.TryRemove(key, out _);
                    
                    return result.UserFacingText; // Return just the text
                }
                catch (JsonException ex)
                {
                    _loggingService.LogError($"Failed to deserialize stored result for Job ID {jobId}: {ex.Message}. Returning raw JSON.");
                    _jobResults.TryRemove(key, out _);
                    // If deserialization fails, maybe return the raw JSON or a generic error?
                    // Returning the raw JSON might expose internal state. Let's return an error message.
                    return "Error retrieving result details."; 
                }
            }
            else
            {
                _loggingService.LogWarning($"No stored result found for Job ID: {jobId}");
                // Check the actual Hangfire job status as a fallback
                var jobState = GetHangfireJobState(jobId);
                 var errorResult = new ProcessedResult { Success = false, ErrorMessage = $"Job result not found. Job State: {jobState}", UserFacingText = "Could not retrieve the result of your action." };
                 return errorResult.UserFacingText; // Return error text
            }
        }

        private string GetHangfireJobState(string jobId)
        {
            try
            {
                var connection = JobStorage.Current.GetConnection();
                var jobData = connection.GetJobData(jobId);
                return jobData?.State ?? "Unknown";
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error getting Hangfire job state for {jobId}: {ex.Message}");
                return "ErrorFetchingState";
            }
        }
    }
} 