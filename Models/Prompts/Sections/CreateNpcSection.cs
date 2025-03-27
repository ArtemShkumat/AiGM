using System.Text;
using System.Text.Json;

namespace AiGMBackEnd.Models.Prompts.Sections
{
    public class CreateNpcSection : PromptSection
    {
        private readonly string _npcName;
        private readonly string _npcId;
        private readonly string _currentLocationId;
        private readonly string _context;

        public CreateNpcSection(string npcName, string npcId, string currentLocationId, string context)
        {
            _npcName = npcName;
            _npcId = npcId;
            _currentLocationId = currentLocationId;
            _context = context;
        }

        public override void AppendTo(StringBuilder builder)
        {
            var creationRequestDetails = new
            {
                name = _npcName,
                id = _npcId,
                currentLocationId = _currentLocationId,
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