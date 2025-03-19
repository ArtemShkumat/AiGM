using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace AiGMBackEnd.Services
{
    public class BackgroundJobService
    {
        private readonly ConcurrentQueue<PromptJob> _jobQueue = new ConcurrentQueue<PromptJob>();
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(0);
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private Task _processingTask;
        private readonly LoggingService _loggingService;
        private readonly PromptService _promptService;
        private readonly AiService _aiService;
        private readonly ResponseProcessingService _responseProcessingService;

        public BackgroundJobService(
            LoggingService loggingService,
            PromptService promptService,
            AiService aiService,
            ResponseProcessingService responseProcessingService)
        {
            _loggingService = loggingService;
            _promptService = promptService;
            _aiService = aiService;
            _responseProcessingService = responseProcessingService;
            
            // Start the background processing task
            _processingTask = Task.Run(ProcessJobsAsync);
        }

        public async Task<string> EnqueuePromptAsync(PromptJob job)
        {
            var tcs = new TaskCompletionSource<string>();
            job.CompletionSource = tcs;
            
            _loggingService.LogInfo($"Enqueueing job for user {job.UserId} with prompt type {job.PromptType}");
            _jobQueue.Enqueue(job);
            _semaphore.Release();
            
            return await tcs.Task;
        }

        private async Task ProcessJobsAsync()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                await _semaphore.WaitAsync(_cancellationTokenSource.Token);
                
                if (_jobQueue.TryDequeue(out var job))
                {
                    try
                    {
                        _loggingService.LogInfo($"Processing job for user {job.UserId}");
                        
                        // 1. Build prompt
                        var prompt = await _promptService.BuildPromptAsync(job.PromptType, job.UserId, job.UserInput);
                        
                        // 2. Call LLM
                        var llmResponse = await _aiService.GetCompletionAsync(prompt, job.PromptType);
                        
                        // 3. Process response
                        var processedResult = await _responseProcessingService.HandleResponseAsync(llmResponse, job.PromptType, job.UserId);
                        
                        // 4. Set result
                        job.CompletionSource.SetResult(processedResult.UserFacingText);
                        
                        _loggingService.LogInfo($"Successfully processed job for user {job.UserId}");
                    }
                    catch (Exception ex)
                    {
                        _loggingService.LogError($"Error processing job: {ex.Message}");
                        job.CompletionSource.SetException(ex);
                    }
                }
            }
        }
    }

    public class PromptJob
    {
        public string UserId { get; set; }
        public string UserInput { get; set; }
        public PromptType PromptType { get; set; } = PromptType.DM;
        public TaskCompletionSource<string> CompletionSource { get; set; }
    }
}
