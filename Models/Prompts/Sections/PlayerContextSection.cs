using System.Text;

namespace AiGMBackEnd.Models.Prompts.Sections
{
    public class PlayerContextSection : PromptSection
    {
        private readonly Player _player;
        private readonly bool _detailed;

        public PlayerContextSection(Player player, bool detailed = true)
        {
            _player = player;
            _detailed = detailed;
        }

        public override void AppendTo(StringBuilder builder)
        {
            builder.AppendLine("# Player Context");
            builder.AppendLine($"Player Name: {_player.Name}");
            
            if (!string.IsNullOrEmpty(_player.Backstory))
            {
                builder.AppendLine($"Background: {_player.Backstory}");
            }
            
            // Add player visual description
            if (_player.VisualDescription != null)
            {
                if (!string.IsNullOrEmpty(_player.VisualDescription.Gender))
                {
                    builder.AppendLine($"Gender: {_player.VisualDescription.Gender}");
                }
                
                if (!string.IsNullOrEmpty(_player.VisualDescription.Body))
                {
                    builder.AppendLine($"Body: {_player.VisualDescription.Body}");
                }
                
                if (!string.IsNullOrEmpty(_player.VisualDescription.VisibleClothing))
                {
                    builder.AppendLine($"Clothing: {_player.VisualDescription.VisibleClothing}");
                }
                
                if (!string.IsNullOrEmpty(_player.VisualDescription.Condition))
                {
                    builder.AppendLine($"Physical Condition: {_player.VisualDescription.Condition}");
                }
            }
            
            // If this is a detailed player context, include RPG elements, inventory, and status effects
            if (_detailed)
            {
                // Add player rpg elements
                if (_player.RpgElements != null && _player.RpgElements.Count > 0)
                {
                    builder.AppendLine("RPG Elements:");
                    foreach (var element in _player.RpgElements)
                    {
                        builder.AppendLine($"- {element.Key}: {element.Value}");
                    }
                }
                
                // Add player inventory
                if (_player.Inventory != null && _player.Inventory.Count > 0)
                {
                    builder.AppendLine("Inventory:");
                    foreach (var item in _player.Inventory)
                    {
                        builder.AppendLine($"- {item.Name} (x{item.Quantity}): {item.Description}");
                    }
                }
                
                // Add player status effects
                if (_player.StatusEffects != null && _player.StatusEffects.Count > 0)
                {
                    builder.AppendLine($"Status Effects: {string.Join(", ", _player.StatusEffects)}");
                }
            }
            
            builder.AppendLine();
        }
    }
} 