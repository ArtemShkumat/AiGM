using System.Text;
using System.Text.Json;

namespace AiGMBackEnd.Models.Prompts.Sections
{
    /// <summary>
    /// Represents a section of the prompt dedicated to recent events summaries.
    /// </summary>
    public class RecentEventsSection : PromptSection
    {
        private readonly RecentEvents _recentEvents;

        /// <summary>
        /// Initializes a new instance of the RecentEventsSection class.
        /// </summary>
        /// <param name="recentEvents">The recent events data to include.</param>
        public RecentEventsSection(RecentEvents recentEvents)
        {
            _recentEvents = recentEvents ?? new RecentEvents(); // Ensure we have an object even if null is passed
        }

        /// <summary>
        /// Appends the formatted recent events summary to the prompt builder.
        /// </summary>
        /// <param name="builder">The StringBuilder constructing the prompt.</param>
        public override void AppendTo(StringBuilder builder)
        {
            // Only append if there are actual event messages
            if (_recentEvents.Messages != null && _recentEvents.Messages.Count > 0)
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true // Makes the JSON output readable
                };
                
                builder.AppendLine("# RECENT EVENTS SUMMARY:"); // Clear label for this section
                builder.AppendLine(JsonSerializer.Serialize(_recentEvents, options));
                builder.AppendLine(); // Add a blank line for separation
            }
        }
    }
} 