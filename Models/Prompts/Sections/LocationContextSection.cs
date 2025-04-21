using System.Text;
using System.Text.Json;
using AiGMBackEnd.Models.Locations;

namespace AiGMBackEnd.Models.Prompts.Sections
{
    public class LocationContextSection : PromptSection
    {
        private readonly Location _location;
        private readonly bool _detailed;

        public LocationContextSection(Location location, bool detailed = true)
        {
            _location = location;
            _detailed = detailed;
        }

        public override void AppendTo(StringBuilder builder)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };           
            
            // Instead of directly serializing the Location base class,
            // identify the specific location type and serialize that
            if (!_detailed)
            {
                // Create a simplified location object with just essential properties
                var simplifiedLocation = new
                {
                    id = _location.Id,
                    name = _location.Name,
                    description = _location.Description,
                    type = _location.Type
                };
                builder.AppendLine(JsonSerializer.Serialize(simplifiedLocation, options));
            }
            else if (_location is Building building)
            {
                builder.AppendLine(JsonSerializer.Serialize(building, options));
            }
            else if (_location is Delve delve)
            {
                builder.AppendLine(JsonSerializer.Serialize(delve, options));
            }
            else if (_location is Settlement settlement)
            {
                builder.AppendLine(JsonSerializer.Serialize(settlement, options));
            }
            else if (_location is Wilds wilds)
            {
                builder.AppendLine(JsonSerializer.Serialize(wilds, options));
            }
            else
            {
                // Fallback to the base Location if we can't determine the specific type
                builder.AppendLine(JsonSerializer.Serialize(_location, options));
            }
            
            builder.AppendLine();
        }
    }
} 