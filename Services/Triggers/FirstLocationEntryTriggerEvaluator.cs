using AiGMBackEnd.Models;
using System.Collections.Generic;

namespace AiGMBackEnd.Services.Triggers
{
    public class FirstLocationEntryTriggerEvaluator : ITriggerEvaluator
    {
        private readonly Services.Storage.IEntityStorageService _entityStorageService;
        
        public FirstLocationEntryTriggerEvaluator(Services.Storage.IEntityStorageService entityStorageService)
        {
            _entityStorageService = entityStorageService;
        }
        
        public EventType HandledTriggerType => EventType.FirstLocationEntry;
        
        public bool ShouldTrigger(Event gameEvent, TriggerContext context)
        {
            if (gameEvent.TriggerType != HandledTriggerType || gameEvent.TriggerValue is not LocationTriggerValue locationTrigger)
            {
                return false;
            }
            
            // First, check if we're in the correct location and just moved there
            bool locationMatch = context.CurrentLocationId == locationTrigger.LocationId && 
                               !string.IsNullOrEmpty(context.PreviousLocationId);
            
            if (!locationMatch)
            {
                return false;
            }
            
            if (!locationTrigger.MustBeFirstVisit)
            {
                // If MustBeFirstVisit is false, trigger whenever the player enters the location
                return true;
            }
            
            // Check if we've already recorded this event as having fired before
            if (gameEvent.Context.TryGetValue("hasVisited", out var hasVisitedObj) && 
                hasVisitedObj is bool hasVisited && hasVisited)
            {
                return false;
            }
            
            // This is the first visit, so return true and update the context
            gameEvent.Context["hasVisited"] = true;
            return true;
        }
    }
} 