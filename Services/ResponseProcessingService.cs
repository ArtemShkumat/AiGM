using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AiGMBackEnd.Models;

namespace AiGMBackEnd.Services
{
    public class ResponseProcessingService
    {
        private readonly StorageService _storageService;
        private readonly LoggingService _loggingService;

        public ResponseProcessingService(
            StorageService storageService,
            LoggingService loggingService)
        {
            _storageService = storageService;
            _loggingService = loggingService;
        }

        public async Task<ProcessedResult> HandleResponseAsync(string llmResponse, string promptType, string userId)
        {
            try
            {
                // Extract hidden JSON and user-facing text
                var (userFacingText, hiddenJson) = ExtractHiddenJson(llmResponse);

                // Process any state updates or entity creation based on the hidden JSON
                if (!string.IsNullOrEmpty(hiddenJson))
                {
                    // TODO: Implement JSON processing logic
                    // - Apply updates to entities
                    // - Create new entities
                    // - Update game state
                }

                return new ProcessedResult
                {
                    UserFacingText = userFacingText,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error processing response: {ex.Message}");
                throw;
            }
        }

        private (string userFacingText, string hiddenJson) ExtractHiddenJson(string llmResponse)
        {
            // Pattern to extract content between <donotshow> tags
            var regex = new Regex(@"(.*?)<donotshow>(.*?)</donotshow>(.*?)", RegexOptions.Singleline);
            var match = regex.Match(llmResponse);

            if (match.Success)
            {
                var beforeTag = match.Groups[1].Value.Trim();
                var jsonContent = match.Groups[2].Value.Trim();
                var afterTag = match.Groups[3].Value.Trim();

                // Combine text before and after the tags
                var userFacingText = (beforeTag + " " + afterTag).Trim();
                
                return (userFacingText, jsonContent);
            }

            // No hidden content found
            return (llmResponse, null);
        }
    }
}
