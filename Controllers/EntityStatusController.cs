using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using AiGMBackEnd.Models;
using AiGMBackEnd.Services;
using AiGMBackEnd.Services.Processors;
using System.Collections.Generic;
using System.Diagnostics;
using System;
using Hangfire;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;

namespace AiGMBackEnd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EntityStatusController : ControllerBase
    {
        private readonly IStatusTrackingService _statusTrackingService;
        private readonly LoggingService _loggingService;
        
        public EntityStatusController(
            IStatusTrackingService statusTrackingService,
            LoggingService loggingService)
        {
            _statusTrackingService = statusTrackingService;
            _loggingService = loggingService;
        }
        
        [HttpGet("{userId}/{entityId}")]
        public async Task<ActionResult<EntityCreationStatus>> GetEntityStatus(string userId, string entityId)
        {
            var status = await _statusTrackingService.GetEntityStatusAsync(userId, entityId);
            
            if (status == null)
            {
                return NotFound();
            }
            
            return status;
        }
        
        [HttpGet("pending/{userId}")]
        public async Task<ActionResult<bool>> HasPendingEntities(string userId)
        {
            return await _statusTrackingService.HasPendingEntitiesAsync(userId);
        }

        [HttpGet("all-pending/{userId}")]
        public async Task<ActionResult<IEnumerable<EntityCreationStatus>>> GetAllPendingEntities(string userId)
        {
            var pendingEntities = await _statusTrackingService.GetAllPendingEntitiesAsync(userId);
            return Ok(pendingEntities);
        }

        [HttpGet("diagnostics/{userId}")]
        public ActionResult<object> GetDiagnostics(string userId)
        {
            // Get Hangfire metrics instead of custom BackgroundJobService metrics
            var monitoringApi = JobStorage.Current.GetMonitoringApi();
            var queues = monitoringApi.Queues();
            var stats = monitoringApi.GetStatistics();
            
            return new
            {
                HangfireStats = new
                {
                    Queued = stats.Enqueued,
                    Processing = stats.Processing,
                    Scheduled = stats.Scheduled,
                    Succeeded = stats.Succeeded,
                    Failed = stats.Failed,
                    Queues = queues
                },
                EntityTracking = new
                {
                    HasPendingEntities = _statusTrackingService.HasPendingEntitiesAsync(userId).Result
                }
            };
        }

        [HttpPost("retry-failed-jobs")]
        public ActionResult<object> RetryFailedJobs()
        {
            _loggingService.LogInfo("Manually retrying failed jobs");
            
            int retriedCount = 0;
            using (var connection = JobStorage.Current.GetConnection())
            {
                var failedList = JobStorage.Current.GetMonitoringApi().FailedJobs(0, int.MaxValue);
                foreach (var job in failedList)
                {
                    BackgroundJob.Requeue(job.Key);
                    retriedCount++;
                }
            }
            
            return new
            {
                Success = true,
                Message = $"Requeued {retriedCount} failed jobs"
            };
        }

        [HttpGet("admin/diagnostics")]
        public ActionResult<object> GetAdminDiagnostics()
        {
            _loggingService.LogInfo("Generating admin diagnostics report");
            
            // Create diagnostic data points using Hangfire APIs
            var monitoringApi = JobStorage.Current.GetMonitoringApi();
            var stats = monitoringApi.GetStatistics();
            var servers = monitoringApi.Servers();
            
            var diagnosticData = new
            {
                HangfireStats = new
                {
                    Servers = servers.Select(s => new 
                    {
                        s.Name,
                        s.Heartbeat,
                        s.Queues,
                        s.StartedAt,
                        s.WorkersCount
                    }),
                    Queued = stats.Enqueued,
                    Processing = stats.Processing,
                    Scheduled = stats.Scheduled,
                    Succeeded = stats.Succeeded,
                    Failed = stats.Failed
                },
                
                SystemInfo = new
                {
                    CurrentTime = DateTime.UtcNow,
                    ProcessStartTime = Process.GetCurrentProcess().StartTime.ToUniversalTime(),
                    ProcessUptime = (DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime()).ToString(),
                    MemoryUsageMB = Process.GetCurrentProcess().WorkingSet64 / (1024 * 1024),
                    ThreadCount = Process.GetCurrentProcess().Threads.Count
                }
            };
            
            return diagnosticData;
        }

        [HttpPost("admin/reset-stuck-entity/{userId}/{entityId}")]
        public async Task<ActionResult<object>> ResetStuckEntity(string userId, string entityId)
        {
            _loggingService.LogInfo($"Manually resetting stuck entity {entityId} for user {userId}");
            
            // Get current status
            var currentStatus = await _statusTrackingService.GetEntityStatusAsync(userId, entityId);
            
            if (currentStatus == null)
            {
                return NotFound(new { Success = false, Message = $"No entity with ID {entityId} found for user {userId}" });
            }
            
            // Reset the entity status to error, which will allow the frontend to proceed
            await _statusTrackingService.UpdateEntityStatusAsync(userId, entityId, "error", "Entity creation reset manually by admin");
            
            return new
            {
                Success = true,
                Message = $"Reset entity {entityId} status from '{currentStatus.Status}' to 'error'",
                PreviousStatus = currentStatus
            };
        }
        
        [HttpPost("admin/delete-jobs")]
        public ActionResult<object> DeleteJobs(string queue = null)
        {
            _loggingService.LogWarning($"Admin requested job deletion for queue: {queue ?? "all"}");
            
            try
            {
                using (var connection = JobStorage.Current.GetConnection())
                {
                    var jobStorageConnection = connection as JobStorageConnection;
                    
                    // Delete all enqueued jobs
                    if (queue == null)
                    {
                        // This is a simplistic implementation and may not work with all Hangfire storage providers
                        var queues = JobStorage.Current.GetMonitoringApi().Queues();
                        foreach (var queueInfo in queues)
                        {
                            // For most storage providers, we can use BackgroundJob.Delete to remove jobs
                            // Instead of trying to directly manipulate the queue
                            var queuedJobs = JobStorage.Current.GetMonitoringApi().EnqueuedJobs(queueInfo.Name, 0, int.MaxValue);
                            foreach (var job in queuedJobs)
                            {
                                BackgroundJob.Delete(job.Key);
                            }
                        }
                    }
                    else
                    {
                        // Delete jobs from specific queue
                        var queuedJobs = JobStorage.Current.GetMonitoringApi().EnqueuedJobs(queue, 0, int.MaxValue);
                        foreach (var job in queuedJobs)
                        {
                            BackgroundJob.Delete(job.Key);
                        }
                    }
                }
                
                return new
                {
                    Success = true,
                    Message = $"Attempted to remove jobs from {(queue == null ? "all queues" : $"queue '{queue}'")}"
                };
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error deleting jobs: {ex.Message}");
                return new
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }
    }
} 