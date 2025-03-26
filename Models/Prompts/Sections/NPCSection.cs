using System.Text;
using System.Text.Json;

namespace AiGMBackEnd.Models.Prompts.Sections
{
    public class NPCSection : PromptSection
    {
        private readonly Npc _npc;
        private readonly bool _detailed;

        public NPCSection(Npc npc, bool detailed = true)
        {
            _npc = npc;
            _detailed = detailed;
        }

        public override void AppendTo(StringBuilder builder)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            
            // If not detailed, we might want to simplify the NPC object
            object npcToSerialize = _npc;
            
            if (!_detailed)
            {
                // Create a simplified NPC with only basic information
                var simplifiedPersonality = new
                {
                    temperament = _npc.Personality?.Temperament,
                    quirks = _npc.Personality?.Quirks
                };
                
                npcToSerialize = new
                {
                    id = _npc.Id,
                    name = _npc.Name,
                    visualDescription = _npc.VisualDescription,
                    personality = simplifiedPersonality,
                    backstory = _npc.Backstory,
                    dispositionTowardsPlayer = _npc.DispositionTowardsPlayer
                };
            }
            
            builder.AppendLine("npc: ");
            builder.AppendLine(JsonSerializer.Serialize(npcToSerialize, options));
            builder.AppendLine();
        }
    }
} 