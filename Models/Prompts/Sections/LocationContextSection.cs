using System.Text;
using System.Text.Json;
using AiGMBackEnd.Models.Locations;

namespace AiGMBackEnd.Models.Prompts.Sections
{
    public class LocationContextSection : PromptSection
    {
        private readonly Location _location;

        public LocationContextSection(Location location)
        {
            _location = location;
        }

        public override void AppendTo(StringBuilder builder)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            
            builder.AppendLine("locationContext: ");
            builder.AppendLine(JsonSerializer.Serialize(_location, options));
            builder.AppendLine();
        }
    }
} 