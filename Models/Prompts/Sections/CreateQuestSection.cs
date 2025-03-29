using System;
using System.Text;
using System.Text.Json;

namespace AiGMBackEnd.Models.Prompts.Sections
{
    public class CreateQuestSection : PromptSection
    {
        private readonly string _questId;
        private readonly string _questName;
        private readonly string _context;
        private readonly bool _detailed;

        public CreateQuestSection(string questId, string questName, string context, bool detailed = true)
        {
            _questId = questId;
            _questName = questName;
            _context = context;
            _detailed = detailed;
        }

        public override void AppendTo(StringBuilder builder)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            object creationRequestDetails;
            
            if (_detailed)
            {
                creationRequestDetails = new
                {
                    name = _questName,
                    id = _questId,
                    context = _context,
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
            }
            else
            {
                creationRequestDetails = new
                {
                    name = _questName,
                    id = _questId,
                    context = _context
                };
            }

            builder.AppendLine("creationRequestDetails:");
            builder.AppendLine(JsonSerializer.Serialize(creationRequestDetails, options));
            builder.AppendLine();
        }
    }
} 