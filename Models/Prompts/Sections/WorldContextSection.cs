using System.Text;
using System.Text.Json;
using System.Linq;

namespace AiGMBackEnd.Models.Prompts.Sections
{
    public class WorldContextSection : PromptSection
    {
        private readonly World _world;
        private readonly bool _includeEntityLists;

        public WorldContextSection(World world, bool includeEntityLists = true)
        {
            _world = world;
            _includeEntityLists = includeEntityLists;
        }

        public override void AppendTo(StringBuilder builder)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            
            // Create a world context object for serialization
            var worldContext = new
            {
                currentTime = _world.GameTime,
                daysSinceStart = _world.DaysSinceStart,
                worldStateEffects = _world.WorldStateEffects,
                lore = _world.Lore,
                locations = _world.Locations,
                npcs = _world.Npcs,
                quests = _world.Quests                
            };
            
            // If entity lists should be included
            if (_includeEntityLists)
            {
                // We'll serialize the full world object
                builder.AppendLine("worldContext: ");
                builder.AppendLine(JsonSerializer.Serialize(_world, options));
            }
            else
            {
                // We'll serialize just the simplified context object
                builder.AppendLine("worldContext: ");
                builder.AppendLine(JsonSerializer.Serialize(worldContext, options));
            }
            
            builder.AppendLine();
        }
    }
} 