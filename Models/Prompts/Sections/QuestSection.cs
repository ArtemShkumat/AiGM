using System.Text;

namespace AiGMBackEnd.Models.Prompts.Sections
{
    public class QuestSection : PromptSection
    {
        private readonly Quest _quest;
        private readonly bool _detailed;

        public QuestSection(Quest quest, bool detailed = true)
        {
            _quest = quest;
            _detailed = detailed;
        }

        public override void AppendTo(StringBuilder builder)
        {
            builder.AppendLine($"## Quest: {_quest.Title} (ID: {_quest.Id})");
            builder.AppendLine($"Core Objective: {_quest.CoreObjective}");
            builder.AppendLine($"Overview: {_quest.Overview}");
            
            if (_detailed)
            {
                // Add NPCs involved
                if (_quest.Npcs != null && _quest.Npcs.Count > 0)
                {
                    builder.AppendLine("Involved NPCs:");
                    foreach (var npc in _quest.Npcs)
                    {
                        builder.AppendLine($"- {npc.Name} ({npc.Role})");
                        builder.AppendLine($"  Motivation: {npc.Motivation}");
                        builder.AppendLine($"  Fears: {npc.Fears}");
                        builder.AppendLine($"  Secrets: {npc.Secrets}");
                    }
                }
                
                // Add rumors and leads
                if (_quest.RumorsAndLeads != null && _quest.RumorsAndLeads.Count > 0)
                {
                    builder.AppendLine("Rumors and Leads:");
                    foreach (var rumor in _quest.RumorsAndLeads)
                    {
                        builder.AppendLine($"- {rumor.Rumor}");
                        builder.AppendLine($"  Source NPC: {rumor.SourceNPC}");
                        builder.AppendLine($"  Source Location: {rumor.SourceLocation}");
                    }
                }
                
                // Add locations involved
                if (_quest.LocationsInvolved != null && _quest.LocationsInvolved.Count > 0)
                {
                    builder.AppendLine($"Locations Involved: {string.Join(", ", _quest.LocationsInvolved)}");
                }
                
                // Add opposing forces
                if (_quest.OpposingForces != null && _quest.OpposingForces.Count > 0)
                {
                    builder.AppendLine("Opposing Forces:");
                    foreach (var force in _quest.OpposingForces)
                    {
                        builder.AppendLine($"- {force.Name} ({force.Role})");
                        builder.AppendLine($"  Motivation: {force.Motivation}");
                        builder.AppendLine($"  Description: {force.Description}");
                    }
                }
                
                // Add challenges
                if (_quest.Challenges != null && _quest.Challenges.Count > 0)
                {
                    builder.AppendLine("Challenges:");
                    foreach (var challenge in _quest.Challenges)
                    {
                        builder.AppendLine($"- {challenge}");
                    }
                }
                
                // Add emotional beats
                if (_quest.EmotionalBeats != null && _quest.EmotionalBeats.Count > 0)
                {
                    builder.AppendLine("Emotional Beats:");
                    foreach (var beat in _quest.EmotionalBeats)
                    {
                        builder.AppendLine($"- {beat}");
                    }
                }
                
                // Add rewards
                if (_quest.Rewards != null)
                {
                    builder.AppendLine("Rewards:");
                    builder.AppendLine($"- Experience: {_quest.Rewards.Experience}");
                    
                    if (_quest.Rewards.Material != null && _quest.Rewards.Material.Count > 0)
                    {
                        builder.AppendLine("  Material Rewards:");
                        foreach (var reward in _quest.Rewards.Material)
                        {
                            builder.AppendLine($"  - {reward}");
                        }
                    }
                    
                    if (_quest.Rewards.Narrative != null && _quest.Rewards.Narrative.Count > 0)
                    {
                        builder.AppendLine("  Narrative Rewards:");
                        foreach (var reward in _quest.Rewards.Narrative)
                        {
                            builder.AppendLine($"  - {reward}");
                        }
                    }
                }
                
                // Add follow-up hooks
                if (_quest.FollowupHooks != null && _quest.FollowupHooks.Count > 0)
                {
                    builder.AppendLine("Follow-up Hooks:");
                    foreach (var hook in _quest.FollowupHooks)
                    {
                        builder.AppendLine($"- {hook}");
                    }
                }
            }
            
            builder.AppendLine();
        }
    }
} 