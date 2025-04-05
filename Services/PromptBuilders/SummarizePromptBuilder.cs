using System;
using System.Threading.Tasks;
using AiGMBackEnd.Models;
using AiGMBackEnd.Models.Prompts;
using AiGMBackEnd.Models.Prompts.Sections;
using System.Text;

namespace AiGMBackEnd.Services.PromptBuilders
{
    public class SummarizePromptBuilder : IPromptBuilder
    {
        private readonly StorageService _storageService;
        private readonly LoggingService _loggingService;

        public SummarizePromptBuilder(
            StorageService storageService,
            LoggingService loggingService)
        {
            _storageService = storageService;
            _loggingService = loggingService;
        }

        public async Task<Prompt> BuildPromptAsync(PromptRequest request)
        {
            try
            {
                _loggingService.LogInfo($"Building summarize prompt for user {request.UserId}");
                
                // Load conversation log to summarize
                var conversationLog = await _storageService.GetConversationLogAsync(request.UserId);
                if (conversationLog == null || conversationLog.Messages.Count == 0)
                {
                    _loggingService.LogWarning($"No conversation log found for user {request.UserId}");
                    throw new InvalidOperationException("No conversation log found to summarize");
                }


                // Step 1: Get the templates
                string systemPrompt = await _storageService.GetSummarizeTemplateAsync("System");
                string outputStructure = await _storageService.GetSummarizeTemplateAsync("OutputStructure");
                string exampleResponses = await _storageService.GetSummarizeTemplateAsync("ExampleResponses");

                var systemPromptBuilder = new StringBuilder();
                systemPromptBuilder.AppendLine(systemPrompt);
                systemPromptBuilder.AppendLine();

                // Step 2: Build the prompt content
                systemPromptBuilder.AppendLine("# CONVERSATION TO SUMMARIZE:");
                systemPromptBuilder.AppendLine();

                // Add output structure and examples
                systemPromptBuilder.AppendLine("# OUTPUT STRUCTURE:");
                systemPromptBuilder.AppendLine(outputStructure);
                systemPromptBuilder.AppendLine();

                systemPromptBuilder.AppendLine("# EXAMPLE SUMMARIES:");
                systemPromptBuilder.AppendLine(exampleResponses);

                // Create a prompt content builder for the summarize prompt
                var promptContentBuilder = new StringBuilder();
                // Add the conversation log to summarize
                new ConversationLogSection(conversationLog).AppendTo(promptContentBuilder);
                promptContentBuilder.AppendLine();

                string promptContent = promptContentBuilder.ToString();

                // Create and return the prompt
                var prompt = new Prompt(systemPrompt, promptContent, PromptType.Summarize);

                return prompt;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error building summarize prompt: {ex.Message}");
                throw;
            }
        }
    }
} 