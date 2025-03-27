using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using AiGMBackEnd.Models;
using AiGMBackEnd.Models.Prompts;

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
        private Func<ResponseProcessingService>? _responseProcessingServiceFactory;
        private readonly object _queueLock = new object();
        private bool _isProcessing = false;
        private bool _isInitialized = false;

        public BackgroundJobService(
            LoggingService loggingService,
            PromptService promptService,
            AiService aiService)
        {
            _loggingService = loggingService;
            _promptService = promptService;
            _aiService = aiService;
        }
        
        public void SetResponseProcessingServiceFactory(Func<ResponseProcessingService> factory)
        {
            _responseProcessingServiceFactory = factory;
            
            if (!_isInitialized)
            {
                _loggingService.LogInfo("Initializing background job processing");
                _processingTask = Task.Run(ProcessJobsAsync);
                _isInitialized = true;
            }
        }

        public async Task<string> EnqueuePromptAsync(PromptRequest request)
        {
            EnsureProcessingTaskIsRunning();
            
            var job = new PromptJob(request);
            _loggingService.LogInfo($"Enqueueing job for user {request.UserId} with prompt type {request.PromptType}");
            
            lock (_queueLock)
            {
                _jobQueue.Enqueue(job);
                _semaphore.Release();
            }
            
            return await job.CompletionSource.Task;
        }

        private void EnsureProcessingTaskIsRunning()
        {
            if (!_isInitialized)
            {
                _loggingService.LogError("Background job service is not properly initialized. ResponseProcessingServiceFactory must be set first.");
                throw new InvalidOperationException("Background job service is not properly initialized");
            }
            
            lock (_queueLock)
            {
                if (_processingTask == null || _processingTask.IsCompleted || _processingTask.IsFaulted || _processingTask.IsCanceled)
                {
                    _loggingService.LogInfo("Restarting job processing task");
                    _processingTask = Task.Run(ProcessJobsAsync);
                }
            }
        }

        private async Task ProcessJobsAsync()
        {
            _loggingService.LogInfo("Background job processing started");
            
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    _isProcessing = false;
                    await _semaphore.WaitAsync(_cancellationTokenSource.Token);
                    _isProcessing = true;
                    
                    while (_jobQueue.TryDequeue(out var job))
                    {
                        try
                        {
                            _loggingService.LogInfo($"Processing job for user {job.Request.UserId} with prompt type {job.Request.PromptType}");
                        
                            // 1. Build prompt
                            var prompt = await _promptService.BuildPromptAsync(job.Request);
                        
                            // 2. Call LLM
                            var llmResponse = await _aiService.GetCompletionAsync(prompt);
                            _loggingService.LogInfo($"LLM response received for {job.Request.PromptType}, length: {llmResponse?.Length ?? 0}");
                        
                            // 3. Process response
                            if (_responseProcessingServiceFactory == null)
                            {
                                throw new InvalidOperationException("ResponseProcessingService factory not set");
                            }
                        
                            var responseProcessingService = _responseProcessingServiceFactory();
                            ProcessedResult processedResult;

                            // Call the appropriate method based on the PromptType
                            if (job.Request.PromptType == PromptType.DM || job.Request.PromptType == PromptType.NPC)
                            {
                                // For DM and NPC responses, we expect user-facing text and optional hidden JSON
                                processedResult = await responseProcessingService.HandleResponseAsync(llmResponse, job.Request.PromptType, job.Request.UserId);
                            }
                            else
                            {
                                // For entity creation responses, we expect pure JSON
                                processedResult = await responseProcessingService.HandleCreateResponseAsync(llmResponse, job.Request.PromptType, job.Request.UserId);
                            }
                        
                            // 4. Set result
                            job.CompletionSource.SetResult(processedResult.UserFacingText);
                            
                            _loggingService.LogInfo($"Successfully processed job for user {job.Request.UserId}, prompt type: {job.Request.PromptType}");
                        }
                        catch (Exception ex)
                        {
                            _loggingService.LogError($"Error processing job for {job.Request.PromptType} ({job.Request.UserId}): {ex.Message}");
                            try
                            {
                                job.CompletionSource.SetResult($"Error processing your request: {ex.Message.Substring(0, Math.Min(100, ex.Message.Length))}");
                            }
                            catch (Exception setEx)
                            {
                                _loggingService.LogError($"Error setting result: {setEx.Message}");
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    _loggingService.LogInfo("Background job processing canceled");
                    break;
                }
                catch (Exception ex)
                {
                    _loggingService.LogError($"Critical error in job processing loop: {ex.Message}");
                    _loggingService.LogError($"Stack trace: {ex.StackTrace}");
                    await Task.Delay(1000);
                }
            }
        }
        
        public int GetQueueLength() => _jobQueue.Count;
        
        public bool IsProcessingActive() => _isProcessing;
        
        public bool IsInitialized() => _isInitialized;
        
        public void KickProcessingTask() => EnsureProcessingTaskIsRunning();
    }

    public class PromptJob
    {
        public PromptRequest Request { get; }
        public TaskCompletionSource<string> CompletionSource { get; }

        public PromptJob(PromptRequest request)
        {
            Request = request;
            CompletionSource = new TaskCompletionSource<string>();
        }
    }
}
