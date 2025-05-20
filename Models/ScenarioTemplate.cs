using System.Collections.Generic;

namespace AiGMBackEnd.Models
{
    public class ScenarioTemplate
    {
        public string TemplateId { get; set; }
        public string TemplateName { get; set; }
        public GameSetting GameSettings { get; set; }
        public List<NpcStub> Npcs { get; set; }
        public List<LocationStub> Locations { get; set; }
        public List<QuestStub> Quests { get; set; }
        public List<EventStub> Events { get; set; }
    }

    public class NpcStub
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class LocationStub
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class QuestStub
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class EventStub
    {
        public string Name { get; set; }
        public string Summary { get; set; }
        public string TriggerType { get; set; }
        public EventTriggerValue TriggerValue { get; set; }
        public string Context { get; set; }
    }

    public class EventTriggerValue
    {
        public string TargetTime { get; set; }
        public string TimeType { get; set; }
        public string LocationId { get; set; }
        public string LocationName { get; set; }
    }
} 