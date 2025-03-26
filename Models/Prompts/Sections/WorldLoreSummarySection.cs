using System.Text;
using AiGMBackEnd.Models;

namespace AiGMBackEnd.Models.Prompts.Sections
{
    public class WorldLoreSummarySection : PromptSection
    {
        private readonly World _world;
        
        public WorldLoreSummarySection(World world)
        {
            _world = world;
        }
        
        public override void AppendTo(StringBuilder builder)
        {
            builder.AppendLine("# Lore");
            
            // Add world lore summary if available
            if (_world.Lore != null && _world.Lore.Count > 0)
            {
                builder.AppendLine($"Summary: {_world.Lore[0].Summary}");
            }
            
            builder.AppendLine();
        }
    }
} 