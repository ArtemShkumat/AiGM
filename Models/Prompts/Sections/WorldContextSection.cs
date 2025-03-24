using System.Text;

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
            builder.AppendLine("# World Context");
            builder.AppendLine($"Current Time: {_world.GameTime}");
            if (_world.WorldStateEffects != null && _world.WorldStateEffects.Count > 0)
            {
                builder.AppendLine("Current World State Effects:");
                foreach (var effect in _world.WorldStateEffects)
                {
                    builder.AppendLine($"- {effect.Key}: {effect.Value}");
                }
            }
            builder.AppendLine($"Days Since Start: {_world.DaysSinceStart}");
                        
            // Add world lore summaries
            if (_world.Lore != null && _world.Lore.Count > 0)
            {
                builder.AppendLine("World Lore:");
                foreach (var lore in _world.Lore)
                {
                    builder.AppendLine($"- {lore.Title}: {lore.Summary}");
                }
            }
            
            if (_includeEntityLists)
            {
                // Add all NPC names and IDs
                if (_world.Npcs != null && _world.Npcs.Count > 0)
                {
                    builder.AppendLine("Existing NPCs:");
                    foreach (var npc in _world.Npcs)
                    {
                        builder.AppendLine($"- {npc.Name} (ID: {npc.Id})");
                    }
                }
                
                // Add all Location names and IDs
                if (_world.Locations != null && _world.Locations.Count > 0)
                {
                    builder.AppendLine("Existing Locations:");
                    foreach (var loc in _world.Locations)
                    {
                        builder.AppendLine($"- {loc.Name} (ID: {loc.Id})");
                    }
                }

                // Add all existing quest names and IDs
                if (_world.Quests != null && _world.Quests.Count > 0)
                {
                    builder.AppendLine("Existing Quests:");
                    foreach (var q in _world.Quests)
                    {
                        builder.AppendLine($"- {q.Title} (ID: {q.Id})");
                    }
                }
            }

            builder.AppendLine();
        }
    }
} 