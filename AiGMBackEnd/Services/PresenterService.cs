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
            // TODO: Implement logic to either use BackgroundJobService or direct call to PromptService
            return "Not implemented yet";
        }
    }
}
