using System.Text;
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
            builder.AppendLine("# Current Location");
            builder.AppendLine($"Location Name: {_location.Name}");
            builder.AppendLine($"Location Type: {_location.Type}");
            builder.AppendLine($"Description: {_location.Description}");
            
            // Add connected locations
            if (_location.ConnectedLocations != null && _location.ConnectedLocations.Count > 0)
            {
                builder.AppendLine("Connected Locations:");
                foreach (var connectedLocation in _location.ConnectedLocations)
                {
                    builder.AppendLine($"- {connectedLocation.Id}: {connectedLocation.Description}");
                }
            }
            
            // Add sublocations
            if (_location.SubLocations != null && _location.SubLocations.Count > 0)
            {
                builder.AppendLine("Sub-Locations:");
                foreach (var subLocation in _location.SubLocations)
                {
                    builder.AppendLine($"- {subLocation.Id}: {subLocation.Description}");
                }
            }
            
            // Add points of interest
            if (_location.PointsOfInterest != null && _location.PointsOfInterest.Count > 0)
            {
                builder.AppendLine("Points of Interest:");
                foreach (var poi in _location.PointsOfInterest)
                {
                    builder.AppendLine($"- {poi.Name}: {poi.Description}");
                }
            }
            
            // Add location items
            if (_location.Items != null && _location.Items.Count > 0)
            {
                builder.AppendLine($"Items Present: {string.Join(", ", _location.Items)}");
            }
            
            builder.AppendLine();
        }
    }
} 