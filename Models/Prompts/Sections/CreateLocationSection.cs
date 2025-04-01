using System;
using System.Text;
using System.Text.Json;

namespace AiGMBackEnd.Models.Prompts.Sections
{
    public class CreateLocationSection : PromptSection
    {
        private readonly string _locationName;
        private readonly string _locationId;
        private readonly string _context;
        private readonly bool _detailed;

        public CreateLocationSection(string locationName, string locationId, string context, bool detailed = true)
        {
            _locationName = locationName;
            _locationId = locationId;
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
                    name = _locationName,
                    id = _locationId,
                    context = _context,
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
            }
            else
            {
                creationRequestDetails = new
                {
                    name = _locationName,
                    id = _locationId,
                    context = _context
                };
            }

            builder.AppendLine("creationRequestDetails:");
            builder.AppendLine(JsonSerializer.Serialize(creationRequestDetails, options));
            builder.AppendLine();
        }
    }
} 