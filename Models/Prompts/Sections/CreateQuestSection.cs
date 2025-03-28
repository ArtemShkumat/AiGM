using System.Text;
using System.Text.Json;

namespace AiGMBackEnd.Models.Prompts.Sections
{
    public class CreateQuestSection : PromptSection
    {
        private readonly string _questId;
        private readonly string _questName;
        private readonly string _currentLocationId;
        private readonly string _context;

        public CreateQuestSection(string questId, string questName,  string context)
        {
            _questId = questId;
            _questName = questName;
            _context = context;
        }

        public override void AppendTo(StringBuilder builder)
        {
            var creationRequestDetails = new
            {
                name = _questName,
                id = _questId,
                context = _context
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            builder.AppendLine("creationRequestDetails:");
            builder.AppendLine(JsonSerializer.Serialize(creationRequestDetails, options));
            builder.AppendLine();
        }
    }
} 