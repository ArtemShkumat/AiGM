using AiGMBackEnd.Models;

namespace AiGMBackEnd.Services.Triggers
{
    public class LocationChangeTriggerEvaluator : ITriggerEvaluator
    {
        public EventType HandledTriggerType => EventType.LocationChange;
        
        public bool ShouldTrigger(Event gameEvent, TriggerContext context)
        {
            if (gameEvent.TriggerType != HandledTriggerType || gameEvent.TriggerValue is not LocationTriggerValue locationTrigger)
            {
                return false;
            }
            
            // Trigger if the current location matches the target and we have a previous location (indicating a location change)
            return context.CurrentLocationId == locationTrigger.LocationId && 
                   !string.IsNullOrEmpty(context.PreviousLocationId);
        }
    }
} 