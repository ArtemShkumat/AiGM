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
        private readonly string _locationType;
        private readonly string _parentLocationId;
        private readonly bool _detailed;

        public CreateLocationSection(
            string locationId,
            string locationName, 
            string locationType, 
            string context, 
            string parentLocationId = null,
            bool detailed = true)
        {
            _locationId = locationId;
            _locationName = locationName;
            _locationType = locationType;
            _context = context;
            _parentLocationId = parentLocationId;
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
                    id = _locationId,
                    name = _locationName,
                    locationType = _locationType,
                    context = _context,
                    parentLocationId = _parentLocationId
                };
            }
            else
            {
                creationRequestDetails = new
                {
                    id = _locationId,
                    name = _locationName,
                    parentLocationId = _parentLocationId
                };
            }

            builder.AppendLine("creationRequestDetails:");
            builder.AppendLine(JsonSerializer.Serialize(creationRequestDetails, options));
            builder.AppendLine();
        }
    }
} 