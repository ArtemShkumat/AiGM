using AiGMBackEnd.Models.Prompts;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using AiGMBackEnd.Services.Processors;
using System.IO;
using static AiGMBackEnd.Services.StorageService;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Hangfire;
using AiGMBackEnd.Models;

namespace AiGMBackEnd.Services
{
    public class PresenterService
    {
        private readonly PromptService _promptService;
        private readonly LoggingService _loggingService;
        private readonly StorageService _storageService;
        private readonly IUpdateProcessor _updateProcessor;
        private readonly IStatusTrackingService _statusTrackingService;
        private readonly HangfireJobsService _hangfireJobsService;

        public PresenterService(
            PromptService promptService,
            LoggingService loggingService,
            StorageService storageService,
            IStatusTrackingService statusTrackingService,
            HangfireJobsService hangfireJobsService,
            IUpdateProcessor updateProcessor)
        {
            _promptService = promptService;
            _loggingService = loggingService;
            _storageService = storageService;
            _statusTrackingService = statusTrackingService;
            _hangfireJobsService = hangfireJobsService;
            _updateProcessor = updateProcessor;
        }

        public async Task<string> HandleUserInputAsync(string userId, string userInput, PromptType promptType, string npcId = null)
        {
            try
            {
                _loggingService.LogInfo($"Handling input for user {userId}");
                
                var prompt = new PromptRequest
                {
                    PromptType = promptType,
                    UserId = userId,
                    UserInput = userInput,
                    NpcId = npcId
                };
                
                // Using Hangfire for background processing
                // Since ProcessUserInputAsync now requires PerformContext which is automatically injected by Hangfire,
                // we need to pass only the prompt parameter when enqueuing
                string jobId = BackgroundJob.Enqueue<HangfireJobsService>(x => 
                    x.ProcessUserInputAsync(prompt, null)); // The null will be replaced by Hangfire with the actual context
                
                _loggingService.LogInfo($"Enqueued user input job for {userId}, job ID: {jobId}");
                
                // Wait for the job to complete using polling approach
                var startTime = DateTime.UtcNow;
                var timeout = TimeSpan.FromMinutes(5);
                var checkInterval = TimeSpan.FromSeconds(1);
                
                // Poll the StatusTrackingService instead of using Hangfire APIs
                while (DateTime.UtcNow - startTime < timeout)
                {
                    // Check job status
                    var jobState = GetHangfireJobState(jobId);
                    if (jobState == "Succeeded")
                    {
                        _loggingService.LogInfo($"Job {jobId} completed successfully");
                        // Use the actual job ID to retrieve the result
                        return await _hangfireJobsService.GetProcessedResultAsync(jobId, userId);
                    }
                    else if (jobState == "Failed")
                    {
                        _loggingService.LogError($"Job {jobId} failed");
                        return "Error processing your request. Please try again later.";
                    }
                    
                    await Task.Delay(checkInterval);
                }
                
                _loggingService.LogError($"Timeout waiting for job {jobId} to complete");
                return "Your request is taking longer than expected to process. Please try again later.";
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error handling user input: {ex.Message}");
                return $"Error processing your request: {ex.Message}";
            }
        }
        
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

        public async Task<int> AutoCreateDanglingReferencesAsync(string userId)
        {
            try
            {
                _loggingService.LogInfo($"Finding and auto-creating dangling references for user {userId}");
                
                // Get all dangling references
                var danglingReferences = await _storageService.FindDanglingReferencesAsync(userId);
                int createdCount = 0;
                
                if (danglingReferences == null || danglingReferences.Count == 0)
                {
                    _loggingService.LogInfo($"No dangling references found for user {userId}");
                    return 0;
                }
                
                _loggingService.LogInfo($"Found {danglingReferences.Count} dangling references for user {userId}");
                
                // Process each dangling reference
                foreach (var reference in danglingReferences)
                {
                    // Build a context message 
                    var contextMessage = $"Create a {reference.ReferenceType} with ID {reference.ReferenceId} that was referenced in {reference.FilePath}.";
                    _loggingService.LogInfo($"Context for dangling reference: {contextMessage}");
                    
                    // Use Hangfire to create the entity
                    string jobId = null;
                    
                    switch (reference.ReferenceType.ToUpper())
                    {
                        case "NPC":
                            var npcName = reference.ReferenceId.Replace("npc_", "").Replace("_", " ");
                            jobId = BackgroundJob.Enqueue(() => 
                                _hangfireJobsService.CreateNpcAsync(userId, reference.ReferenceId, npcName, contextMessage, null, false));
                            break;
                            
                        case "LOCATION":
                            var locName = reference.ReferenceId.Replace("loc_", "").Replace("_", " ");
                            jobId = BackgroundJob.Enqueue(() => 
                                _hangfireJobsService.CreateLocationAsync(userId, reference.ReferenceId, locName, "BUILDING", contextMessage, null, false));
                            break;
                            
                        case "QUEST":
                            var questName = reference.ReferenceId.Replace("quest_", "").Replace("_", " ");
                            jobId = BackgroundJob.Enqueue(() => 
                                _hangfireJobsService.CreateQuestAsync(userId, reference.ReferenceId, questName, contextMessage));
                            break;
                            
                        default:
                            _loggingService.LogWarning($"Unknown reference type: {reference.ReferenceType}");
                            continue;
                    }
                    
                    if (!string.IsNullOrEmpty(jobId))
                    {
                        _loggingService.LogInfo($"Created job for dangling reference: {reference.ReferenceId}, job ID: {jobId}");
                        createdCount++;
                    }
                }
                
                return createdCount;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error auto-creating dangling references for user {userId}: {ex.Message}");
                throw;
            }
        }

        // Helper method to extract entity type from file path
        private string GetEntityTypeFromPath(string filePath)
        {
            string[] parts = filePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            
            // Find the relevant directory in the path
            foreach (var part in parts)
            {
                if (part.Equals("npcs", StringComparison.OrdinalIgnoreCase) ||
                    part.Equals("locations", StringComparison.OrdinalIgnoreCase) ||
                    part.Equals("quests", StringComparison.OrdinalIgnoreCase))
                {
                    return part;
                }
            }
            
            return string.Empty;
        }

        public async Task<string> CreateScenarioAsync(CreateScenarioRequest request, bool isStartingScenario = false)
        {
            try
            {
                _loggingService.LogInfo($"Starting scenario creation for prompt: {request.ScenarioPrompt}");
                
                // Generate a scenario ID
                string scenarioId = $"scenario_{Guid.NewGuid().ToString()}";
                
                // Use admin user ID for starting scenarios, otherwise require a user ID
                string userId = isStartingScenario ? "admin" : string.Empty;
                
                // Create the prompt request for scenario generation
                var promptRequest = new PromptRequest
                {
                    PromptType = PromptType.CreateScenario,
                    UserId = userId,
                    UserInput = request.ScenarioPrompt,
                    ScenarioId = scenarioId
                };
                
                // Using Hangfire for background processing
                string jobId = BackgroundJob.Enqueue<HangfireJobsService>(x => 
                    x.CreateScenarioAsync(userId, scenarioId, request.ScenarioPrompt, isStartingScenario));
                
                _loggingService.LogInfo($"Enqueued {(isStartingScenario ? "starting " : "")}scenario creation job, ID: {jobId}");
                
                return scenarioId;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error creating scenario: {ex.Message}");
                throw;
            }
        }
    }
}
