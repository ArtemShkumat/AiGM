using AiGMBackEnd.Models;

namespace AiGMBackEnd.Services.Triggers
{
    public interface ITriggerEvaluator
    {
        EventType HandledTriggerType { get; }
        
        bool ShouldTrigger(Event gameEvent, TriggerContext context);
    }
} 