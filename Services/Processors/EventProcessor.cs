using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AiGMBackEnd.Models;
using AiGMBackEnd.Services.Storage;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace AiGMBackEnd.Services.Processors
{
    public class EventProcessor : IEventProcessor
    {
        private readonly IEventStorageService _eventStorageService;
        private readonly ILogger<EventProcessor> _logger;
        
        public EventProcessor(IEventStorageService eventStorageService, ILogger<EventProcessor> logger)
        {
            _eventStorageService = eventStorageService;
            _logger = logger;
        }
        
        public async Task ProcessAsync(JObject data, string userId)
        {
            // Validation
            string validationError = await ValidateEventCreationData(data);
            if (!string.IsNullOrEmpty(validationError))
            {
                _logger.LogError("Event creation validation failed: {Error}", validationError);
                return;
            }
            
            try
            {
                // Convert from JObject to Event
                Event gameEvent = new Event
                {
                    Id = Guid.NewGuid().ToString(),
                    Summary = data["summary"]?.ToString(),
                    TriggerType = ParseEventType(data["triggerType"]?.ToString()),
                    TriggerValue = ParseTriggerValue(data),
                    Status = EventStatus.Active,
                    CreationTime = DateTimeOffset.UtcNow,
                    Context = new Dictionary<string, object>()
                };
                
                // Save the Event
                await _eventStorageService.SaveEventAsync(userId, gameEvent);
                _logger.LogInformation("Created new event {EventId} for user {UserId}", gameEvent.Id, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating event for user {UserId}", userId);
            }
        }
        
        public async Task<string> ValidateEventCreationData(JObject data)
        {
            // Check required fields
            if (data == null)
            {
                return "Event creation data is null";
            }
            
            if (string.IsNullOrWhiteSpace(data["summary"]?.ToString()))
            {
                return "Event summary is required";
            }
            
            if (string.IsNullOrWhiteSpace(data["triggerType"]?.ToString()))
            {
                return "Event trigger type is required";
            }
            
            if (data["triggerValue"] == null)
            {
                return "Event trigger value is required";
            }
            
            // Validate trigger type
            string triggerTypeStr = data["triggerType"].ToString();
            if (!Enum.TryParse<EventType>(triggerTypeStr, true, out var triggerType))
            {
                return $"Invalid trigger type: {triggerTypeStr}";
            }
            
            // Validate trigger value based on trigger type
            JObject triggerValueObj = data["triggerValue"] as JObject;
            if (triggerValueObj == null)
            {
                return "Trigger value must be an object";
            }
            
            // Validate that the trigger value is appropriate for the trigger type
            switch (triggerType)
            {
                case EventType.Time:
                    if (triggerValueObj["triggerTime"] == null)
                    {
                        return "Time trigger requires a 'triggerTime' field";
                    }
                    
                    if (!DateTimeOffset.TryParse(triggerValueObj["triggerTime"].ToString(), out _))
                    {
                        return "Invalid triggerTime format";
                    }
                    break;
                    
                case EventType.LocationChange:
                case EventType.FirstLocationEntry:
                    if (string.IsNullOrWhiteSpace(triggerValueObj["locationId"]?.ToString()))
                    {
                        return "Location trigger requires a 'locationId' field";
                    }
                    break;
                    
                default:
                    return $"Unsupported trigger type: {triggerType}";
            }
            
            return null; // No validation errors
        }
        
        private EventType ParseEventType(string typeStr)
        {
            if (Enum.TryParse<EventType>(typeStr, true, out var result))
            {
                return result;
            }
            
            _logger.LogWarning("Unknown event type: {Type}, defaulting to Time", typeStr);
            return EventType.Time;
        }
        
        private TriggerValue ParseTriggerValue(JObject data)
        {
            string triggerTypeStr = data["triggerType"].ToString();
            JObject triggerValueObj = data["triggerValue"] as JObject;
            
            if (triggerValueObj == null)
            {
                throw new ArgumentException("Trigger value must be an object");
            }
            
            if (triggerTypeStr.Equals("Time", StringComparison.OrdinalIgnoreCase))
            {
                string triggerTimeStr = triggerValueObj["triggerTime"].ToString();
                return new TimeTriggerValue
                {
                    TriggerTime = DateTimeOffset.Parse(triggerTimeStr)
                };
            }
            else if (triggerTypeStr.Equals("LocationChange", StringComparison.OrdinalIgnoreCase) ||
                     triggerTypeStr.Equals("FirstLocationEntry", StringComparison.OrdinalIgnoreCase))
            {
                string locationId = triggerValueObj["locationId"].ToString();
                bool mustBeFirstVisit = false;
                
                if (triggerValueObj["mustBeFirstVisit"] != null)
                {
                    bool.TryParse(triggerValueObj["mustBeFirstVisit"].ToString(), out mustBeFirstVisit);
                }
                
                return new LocationTriggerValue
                {
                    LocationId = locationId,
                    MustBeFirstVisit = mustBeFirstVisit || triggerTypeStr.Equals("FirstLocationEntry", StringComparison.OrdinalIgnoreCase)
                };
            }
            
            throw new ArgumentException($"Unsupported trigger type: {triggerTypeStr}");
        }
    }
} 