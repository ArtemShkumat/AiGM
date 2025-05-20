using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AiGMBackEnd.Models;

namespace AiGMBackEnd.Services.Storage
{
    public interface IEventStorageService
    {
        Task<List<Event>> GetActiveEventsAsync(string userId);
        Task<List<Event>> GetAllEventsAsync(string userId);
        Task<Event> GetEventAsync(string userId, string eventId);
        Task SaveEventAsync(string userId, Event gameEvent);
        Task UpdateEventStatusAsync(string userId, string eventId, EventStatus status);
        Task<bool> DeleteEventAsync(string userId, string eventId);
        Task<bool> UpdateEventAsync(string userId, string eventId, Action<Event> updateAction);
        Task PurgeOldCompletedEventsAsync(string userId, TimeSpan retentionPeriod);
    }
} 