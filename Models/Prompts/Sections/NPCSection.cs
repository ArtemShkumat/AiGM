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
            
            object npcToSerialize = _npc;
            
            if (!_detailed)
            {
                npcToSerialize = new
                {
                    id = _npc.Id,
                    type = _npc.Type,
                    name = _npc.Name,
                    visualDescription = _npc.VisualDescription,
                    backstory = _npc.Backstory,
                    race = _npc.Race,
                    age = _npc.Age,
                    currentGoal = _npc.CurrentGoal,
                    dispositionTowardsPlayer = _npc.DispositionTowardsPlayer
                };
            }
            
            builder.AppendLine("npc: ");
            builder.AppendLine(JsonSerializer.Serialize(npcToSerialize, options));
            builder.AppendLine();
        }
    }
} 