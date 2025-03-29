using System.Text;
using AiGMBackEnd.Models;

namespace AiGMBackEnd.Models.Prompts.Sections
{
    public class WorldLoreSummarySection : PromptSection
    {
        private readonly World _world;
        private readonly bool _detailed;
        
        public WorldLoreSummarySection(World world, bool detailed = true)
        {
            _world = world;
            _detailed = detailed;
        }
        
        public override void AppendTo(StringBuilder builder)
        {
            builder.AppendLine("# Lore");
            
            // Add world lore summary if available
            if (_world.Lore != null && _world.Lore.Count > 0)
            {
                if (_detailed)
                {
                    // Include full lore details in detailed mode
                    foreach (var loreEntry in _world.Lore)
                    {
                        builder.AppendLine($"Title: {loreEntry.Title}");
                        builder.AppendLine($"Summary: {loreEntry.Summary}");
                        builder.AppendLine();
                    }
                }
                else
                {
                    // Only include the summary in non-detailed mode
                    builder.AppendLine($"Summary: {_world.Lore[0].Summary}");
                }
            }
            
            builder.AppendLine();
        }
    }
} 