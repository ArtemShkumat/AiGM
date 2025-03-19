using System;
using System.Threading.Tasks;

namespace AiGMBackEnd.Services
{
    public class PresenterService
    {
        private readonly PromptService _promptService;
        private readonly AiService _aiService;
        private readonly ResponseProcessingService _responseProcessingService;
        private readonly BackgroundJobService _backgroundJobService;
        private readonly LoggingService _loggingService;

        public PresenterService(
            PromptService promptService,
            AiService aiService,
            ResponseProcessingService responseProcessingService,
            BackgroundJobService backgroundJobService,
            LoggingService loggingService)
        {
            _promptService = promptService;
            _aiService = aiService;
            _responseProcessingService = responseProcessingService;
            _backgroundJobService = backgroundJobService;
            _loggingService = loggingService;
        }

        public async Task<string> HandleUserInputAsync(string userId, string userInput)
        {
            try
            {
                _loggingService.LogInfo($"Handling input for user {userId}: {userInput}");
                
                // Determine if this should be a DM prompt or NPC prompt
                // For now, we'll just use "DM" as the default prompt type
                var promptType = "DM";
                
                // For now, we'll use the background job service for all requests
                // In the future, we might want to bypass it for simple requests
                var job = new PromptJob
                {
                    UserId = userId,
                    UserInput = userInput,
                    PromptType = promptType
                };
                
                var response = await _backgroundJobService.EnqueuePromptAsync(job);
                
                _loggingService.LogInfo($"Completed handling input for user {userId}");
                
                return response;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error handling user input: {ex.Message}");
                return $"Error processing your request: {ex.Message}";
            }
        }
    }
}
