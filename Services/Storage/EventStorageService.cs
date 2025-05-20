using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AiGMBackEnd.Models;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace AiGMBackEnd.Services.Storage
{
    public class EventStorageService : IEventStorageService
    {
        private readonly IBaseStorageService _baseStorageService;
        private readonly ILogger<EventStorageService> _logger;
        
        public EventStorageService(IBaseStorageService baseStorageService, ILogger<EventStorageService> logger)
        {
            _baseStorageService = baseStorageService;
            _logger = logger;
        }
        
        public async Task<List<Event>> GetActiveEventsAsync(string userId)
        {
            try
            {
                var allEvents = await GetAllEventsAsync(userId);
                return allEvents.Where(e => e.Status == EventStatus.Active).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active events for user {UserId}", userId);
                return new List<Event>();
            }
        }
        
        public async Task<List<Event>> GetAllEventsAsync(string userId)
        {
            try
            {
                // Since BaseStorageService doesn't have a method to get all files in a directory,
                // we need to implement this differently. We'll use Directory.GetFiles directly.
                string eventsDirectory = Path.Combine("Data", "userData", userId, "events");
                
                // Ensure the directory exists
                if (!Directory.Exists(eventsDirectory))
                {
                    Directory.CreateDirectory(eventsDirectory);
                    return new List<Event>();
                }
                
                var eventFiles = Directory.GetFiles(eventsDirectory, "*.json");
                List<Event> events = new List<Event>();
                
                foreach (var filePath in eventFiles)
                {
                    try
                    {
                        // Extract just the filename without extension for the fileId
                        string fileName = Path.GetFileNameWithoutExtension(filePath);
                        var gameEvent = await _baseStorageService.LoadAsync<Event>(userId, fileName);
                        if (gameEvent != null)
                        {
                            events.Add(gameEvent);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error loading event from file {FilePath}", filePath);
                    }
                }
                
                return events;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all events for user {UserId}", userId);
                return new List<Event>();
            }
        }
        
        public async Task<Event> GetEventAsync(string userId, string eventId)
        {
            return await _baseStorageService.LoadAsync<Event>(userId, GetEventFileId(eventId));
        }
        
        public async Task SaveEventAsync(string userId, Event gameEvent)
        {
            if (string.IsNullOrEmpty(gameEvent.Id))
            {
                gameEvent.Id = Guid.NewGuid().ToString();
            }
            
            if (gameEvent.CreationTime == default)
            {
                gameEvent.CreationTime = DateTimeOffset.UtcNow;
            }
            
            await _baseStorageService.SaveAsync(userId, GetEventFileId(gameEvent.Id), gameEvent);
        }
        
        public async Task UpdateEventStatusAsync(string userId, string eventId, EventStatus status)
        {
            await UpdateEventAsync(userId, eventId, e => 
            {
                e.Status = status;
                if (status == EventStatus.Completed)
                {
                    e.CompletionTime = DateTimeOffset.UtcNow;
                }
            });
        }
        
        public async Task<bool> DeleteEventAsync(string userId, string eventId)
        {
            try
            {
                string filePath = _baseStorageService.GetFilePath(userId, GetEventFileId(eventId));
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting event {EventId} for user {UserId}", eventId, userId);
                return false;
            }
        }
        
        public async Task<bool> UpdateEventAsync(string userId, string eventId, Action<Event> updateAction)
        {
            try
            {
                var fileId = GetEventFileId(eventId);
                var gameEvent = await _baseStorageService.LoadAsync<Event>(userId, fileId);
                if (gameEvent == null)
                {
                    return false;
                }
                
                updateAction(gameEvent);
                await _baseStorageService.SaveAsync(userId, fileId, gameEvent);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating event {EventId} for user {UserId}", eventId, userId);
                return false;
            }
        }
        
        public async Task PurgeOldCompletedEventsAsync(string userId, TimeSpan retentionPeriod)
        {
            try
            {
                var events = await GetAllEventsAsync(userId);
                if (events == null || !events.Any())
                {
                    _logger.LogInformation("No events found for user {UserId} to purge", userId);
                    return;
                }

                var cutoffDate = DateTime.UtcNow.Subtract(retentionPeriod);
                var eventsToRemove = events
                    .Where(e => e.Status == EventStatus.Completed && e.CompletionTime.HasValue && e.CompletionTime.Value < cutoffDate)
                    .ToList();

                if (!eventsToRemove.Any())
                {
                    _logger.LogInformation("No old completed events to purge for user {UserId}", userId);
                    return;
                }

                _logger.LogInformation("Purging {Count} old completed events for user {UserId}", eventsToRemove.Count, userId);
                
                // Remove each event
                foreach (var eventToRemove in eventsToRemove)
                {
                    await DeleteEventAsync(userId, eventToRemove.Id);
                }
                
                _logger.LogInformation("Successfully purged {Count} old events for user {UserId}", eventsToRemove.Count, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error purging old events for user {UserId}: {Message}", userId, ex.Message);
                throw;
            }
        }
        
        private string GetEventFileId(string eventId)
        {
            return Path.Combine("events", eventId);
        }
    }
} 