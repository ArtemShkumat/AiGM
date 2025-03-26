using System.Text;
using System.Text.Json;

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
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            
            // If not detailed, we might want to simplify the quest object
            var questToSerialize = _quest;
            
            if (!_detailed)
            {
                // Create a simplified quest with only basic information
                questToSerialize = new Quest
                {
                    Id = _quest.Id,
                    Title = _quest.Title,
                    CoreObjective = _quest.CoreObjective,
                    Overview = _quest.Overview
                };
            }
            
            builder.AppendLine("quest: ");
            builder.AppendLine(JsonSerializer.Serialize(questToSerialize, options));
            builder.AppendLine();
        }
    }
} 