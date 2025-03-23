using System.Text;

namespace AiGMBackEnd.Models.Prompts.Sections
{
    public class QuestSection : PromptSection
    {
        private readonly Quest _quest;
        private readonly bool _detailed;

        public QuestSection(Quest quest, bool detailed = true)
        {
            _quest = quest;
            _detailed = detailed;
        }

        public override void AppendTo(StringBuilder builder)
        {
            builder.AppendLine($"## Quest: {_quest.Title} (ID: {_quest.Id})");
            builder.AppendLine($"Description: {_quest.QuestDescription}");
            
            if (!string.IsNullOrEmpty(_quest.CurrentProgress))
            {
                builder.AppendLine($"Current Progress: {_quest.CurrentProgress}");
            }
            
            if (_detailed)
            {
                // Add achievement conditions
                if (_quest.AchievementConditions != null && _quest.AchievementConditions.Count > 0)
                {
                    builder.AppendLine("Achievement Conditions:");
                    foreach (var condition in _quest.AchievementConditions)
                    {
                        builder.AppendLine($"- {condition}");
                    }
                }
                
                // Add fail conditions
                if (_quest.FailConditions != null && _quest.FailConditions.Count > 0)
                {
                    builder.AppendLine("Fail Conditions:");
                    foreach (var condition in _quest.FailConditions)
                    {
                        builder.AppendLine($"- {condition}");
                    }
                }
            }
            
            // Add involved locations
            if (_quest.InvolvedLocations != null && _quest.InvolvedLocations.Count > 0)
            {
                builder.AppendLine($"Involved Locations: {string.Join(", ", _quest.InvolvedLocations)}");
            }
            
            // Add involved NPCs
            if (_quest.InvolvedNpcs != null && _quest.InvolvedNpcs.Count > 0)
            {
                builder.AppendLine($"Involved NPCs: {string.Join(", ", _quest.InvolvedNpcs)}");
            }
            
            if (_detailed)
            {
                // Add quest log if available
                if (_quest.QuestLog != null && _quest.QuestLog.Count > 0)
                {
                    builder.AppendLine("Quest Log:");
                    foreach (var entry in _quest.QuestLog)
                    {
                        builder.AppendLine($"- [{entry.Timestamp}] {entry.Event}: {entry.Description}");
                    }
                }
            }
            
            builder.AppendLine();
        }
    }
} 