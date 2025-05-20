using AiGMBackEnd.Models;

namespace AiGMBackEnd.Services.Triggers
{
    public class TimeTriggerEvaluator : ITriggerEvaluator
    {
        public EventType HandledTriggerType => EventType.Time;
        
        public bool ShouldTrigger(Event gameEvent, TriggerContext context)
        {
            if (gameEvent.TriggerType != HandledTriggerType || gameEvent.TriggerValue is not TimeTriggerValue timeTrigger)
            {
                return false;
            }
            
            return context.CurrentTime >= timeTrigger.TriggerTime;
        }
    }
} 