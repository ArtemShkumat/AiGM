using System.Text;
using System.Text.Json;

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
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            
            // If detailed is true, serialize the entire Player object
            // If detailed is false, use a simplified version with specific properties
            object playerToSerialize = _player;
            
            if (!_detailed)
            {
                // Create a simplified player object with limited properties
                playerToSerialize = new
                {
                    type = _player.Type,
                    visualDescription = _player.VisualDescription,
                    age = _player.Age,
                    statusEffects = _player.StatusEffects,
                    inventory = _player.Inventory,
                    currencies = _player.Currencies
                };
            }
            
            builder.AppendLine("playerContext: ");
            builder.AppendLine(JsonSerializer.Serialize(playerToSerialize, options));
            builder.AppendLine();
        }
    }
} 