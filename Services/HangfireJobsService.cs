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

        public HangfireJobsService(
            LoggingService loggingService,
            PromptService promptService,
            AiService aiService,
            ResponseProcessingService responseProcessingService,
            IStatusTrackingService statusTrackingService,
            StorageService storageService,
            INPCProcessor npcProcessor,
            ILocationProcessor locationProcessor,
            IQuestProcessor questProcessor)
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
        }

        /// <summary>
        /// Creates an entity based on the prompt type and context
        /// </summary>
        public async Task CreateEntityAsync(string userId, string entityId, string entityType, PromptRequest request)
        {
            try
            {
                _loggingService.LogInfo($"Starting entity creation job for {entityType} {entityId}");
                
                // Register that we're creating this entity
                await _statusTrackingService.RegisterEntityCreationAsync(userId, entityId, entityType);
                
                // 1. Build prompt
                var prompt = await _promptService.BuildPromptAsync(request);
                _loggingService.LogInfo($"Built prompt for entity creation {entityType} {entityId}");
                
                // 2. Call LLM
                var llmResponse = await _aiService.GetCompletionAsync(prompt);
                _loggingService.LogInfo($"LLM response received for {entityType} {entityId}, length: {llmResponse?.Length ?? 0}");
                
                // 3. Process the response
                var processedResult = await _responseProcessingService.HandleCreateResponseAsync(llmResponse, request.PromptType, userId);
                
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
        public async Task CreateNpcAsync(string userId, string npcId, string npcName, string context, string currentLocationId)
        {
            var request = new PromptRequest 
            { 
                PromptType = PromptType.CreateNPC,
                UserId = userId,
                Context = context,
                NpcLocation = currentLocationId,
                NpcName = npcName,
                NpcId = npcId
            };
            
            await CreateEntityAsync(userId, npcId, "npc", request);
        }
        
        /// <summary>
        /// Creates a location entity
        /// </summary>
        public async Task CreateLocationAsync(string userId, string locationId, string locationName, string locationType, string context)
        {
            var request = new PromptRequest 
            { 
                PromptType = PromptType.CreateLocation,
                UserId = userId,
                LocationId = locationId,
                LocationName = locationName,
                LocationType = locationType,
                Context = context
            };
            
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
        /// Background job to process summarization
        /// </summary>
        public async Task ProcessSummarizationJobAsync(PromptRequest request)
        {
            try
            {
                _loggingService.LogInfo($"Processing summarization job for user {request.UserId}");
                
                // 1. Build the summarization prompt
                var prompt = await _promptService.BuildPromptAsync(request);
                _loggingService.LogInfo($"Built summarization prompt for user {request.UserId}");
                
                // 2. Call LLM to generate summary
                var llmResponse = await _aiService.GetCompletionAsync(prompt);
                _loggingService.LogInfo($"LLM response received for summarization, length: {llmResponse?.Length ?? 0}");
                
                // 3. Process the summary response
                await _responseProcessingService.HandleSummaryResponseAsync(llmResponse, request.UserId);
                
                _loggingService.LogInfo($"Successfully processed summarization for user {request.UserId}");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error processing summarization job: {ex.Message}");
                throw;
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
                
                // Additional logging for DM/NPC responses to debug potential issues
                if (request.PromptType == PromptType.DM || request.PromptType == PromptType.NPC)
                {
                    _loggingService.LogInfo($"Raw LLM response for {request.PromptType}: {llmResponse}");
                }
                
                // 3. Process the response
                ProcessedResult processedResult;
                if (request.PromptType == PromptType.DM || request.PromptType == PromptType.NPC)
                {
                    _loggingService.LogInfo($"Processing {request.PromptType} response (Job ID: {jobId})");
                    
                    // The response is now expected to be a complete JSON object that conforms to the DmResponse schema
                    // HandleResponseAsync will deserialize this JSON directly into a DmResponse object
                    processedResult = await _responseProcessingService.HandleResponseAsync(llmResponse, request.PromptType, request.UserId, request.NpcId);
                }
                else
                {
                    _loggingService.LogInfo($"Processing creation response for {request.PromptType} (Job ID: {jobId})");
                    processedResult = await _responseProcessingService.HandleCreateResponseAsync(llmResponse, request.PromptType, request.UserId);
                }
                
                // 4. Sync world with entities
                await _storageService.SyncWorldWithEntitiesAsync(request.UserId);
                
                // Store the result in our dictionary using the Job ID as the key
                string key = $"job_result:{jobId}"; // Key is now based on Job ID
                _jobResults[key] = processedResult.UserFacingText;
                
                _loggingService.LogInfo($"Successfully processed user input for {request.PromptType}, stored result with key {key} (Job ID: {jobId})");
                return processedResult.UserFacingText;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error processing user input: {ex.Message}");
                // Optionally store error state if needed
                return $"Error processing your request: {ex.Message}";
            }
        }
        
        /// <summary>
        /// Gets the processed result for a job. This is used when we need to retrieve
        /// the result from a completed Hangfire job.
        /// </summary>
        public async Task<string> GetProcessedResultAsync(string jobId, string userId)
        {
            try
            {
                _loggingService.LogInfo($"Getting processed result for job {jobId}");
                
                // Try to get the result from our dictionary using the Job ID
                string key = $"job_result:{jobId}";
                if (_jobResults.TryRemove(key, out string result))
                {
                    _loggingService.LogInfo($"Found and removed stored result for Job ID: {jobId}");
                    return result;
                }
                
                _loggingService.LogWarning($"Could not find stored result for Job ID: {jobId}");

                // Fallback: Check Hangfire job status
                var jobState = GetHangfireJobState(jobId);
                if (jobState == "Failed")
                {
                    // Optionally retrieve error details from Hangfire if needed
                    return "Error processing your request. Please try again later.";
                }

                // Fallback: Check if there are pending entities for the user
                bool hasPendingEntities = await _statusTrackingService.HasPendingEntitiesAsync(userId);
                if (hasPendingEntities)
                {
                    return "Your request was processed, but some entities are still being created in the background.";
                }
                else
                {
                    return "Your request was processed successfully, but the specific result is no longer available.";
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error getting processed result for job {jobId}: {ex.Message}");
                return "There was an error retrieving the processed result.";
            }
        }

        // Helper method to get job state (you might already have this)
        private string GetHangfireJobState(string jobId)
        {
            try
            {
                using (var connection = JobStorage.Current.GetConnection())
                {
                    var jobData = connection.GetJobData(jobId);
                    return jobData?.State;
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error getting Hangfire job state for {jobId}: {ex.Message}");
                return null;
            }
        }
    }
} 