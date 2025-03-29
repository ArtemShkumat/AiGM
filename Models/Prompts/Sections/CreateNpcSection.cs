using System;
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
        private readonly bool _detailed;

        public CreateNpcSection(string npcName, string npcId, string currentLocationId, string context, bool detailed = true)
        {
            _npcName = npcName;
            _npcId = npcId;
            _currentLocationId = currentLocationId;
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
                    name = _npcName,
                    id = _npcId,
                    currentLocationId = _currentLocationId,
                    context = _context,
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
            }
            else
            {
                creationRequestDetails = new
                {
                    name = _npcName,
                    id = _npcId,
                    currentLocationId = _currentLocationId,
                    context = _context
                };
            }

            builder.AppendLine("creationRequestDetails:");
            builder.AppendLine(JsonSerializer.Serialize(creationRequestDetails, options));
            builder.AppendLine();
        }
    }
} 