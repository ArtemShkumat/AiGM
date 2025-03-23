using System.Text;

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
            builder.AppendLine($"## NPC: {_npc.Name} (ID: {_npc.Id})");
            
            // Add NPC visual description
            if (_npc.VisualDescription != null)
            {
                builder.AppendLine($"Appearance: {_npc.VisualDescription.Gender} {_npc.VisualDescription.Body} wearing {_npc.VisualDescription.VisibleClothing}, {_npc.VisualDescription.Condition}");
            }
            
            // Add NPC personality
            if (_npc.Personality != null)
            {
                builder.AppendLine($"Personality: {_npc.Personality.Temperament}, {_npc.Personality.Quirks}");
                if (!string.IsNullOrEmpty(_npc.Personality.Quirks))
                {
                    builder.AppendLine($"Quirks: {_npc.Personality.Quirks}");
                }
                
                if (_detailed)
                {
                    if (!string.IsNullOrEmpty(_npc.Personality.Motivations))
                    {
                        builder.AppendLine($"Motivations: {_npc.Personality.Motivations}");
                    }
                    
                    if (!string.IsNullOrEmpty(_npc.Personality.Fears))
                    {
                        builder.AppendLine($"Fears: {_npc.Personality.Fears}");
                    }
                    
                    if (_npc.Personality.Secrets != null && _npc.Personality.Secrets.Count > 0)
                    {
                        builder.AppendLine($"Secrets: {string.Join(", ", _npc.Personality.Secrets)}");
                    }
                }
            }
            
            // Add backstory
            if (!string.IsNullOrEmpty(_npc.Backstory))
            {
                builder.AppendLine($"Backstory: {_npc.Backstory}");
            }
            
            // Add disposition towards player
            if (!string.IsNullOrEmpty(_npc.DispositionTowardsPlayer))
            {
                builder.AppendLine($"Disposition: {_npc.DispositionTowardsPlayer}");
            }
            
            // Add relevant quest involvement
            if (_npc.QuestInvolvement != null && _npc.QuestInvolvement.Count > 0)
            {
                builder.AppendLine($"Quest Involvement: {string.Join(", ", _npc.QuestInvolvement)}");
            }
            
            builder.AppendLine();
        }
    }
} 