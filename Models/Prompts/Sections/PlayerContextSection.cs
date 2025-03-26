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
            
            // If not detailed, we might want to simplify the player object
            object playerToSerialize = _player;
            
            if (!_detailed)
            {
                // Create a simplified player object without detailed RPG elements, inventory, etc.
                playerToSerialize = new
                {
                    name = _player.Name,
                    currentLocationId = _player.CurrentLocationId,
                    backstory = _player.Backstory,
                    visualDescription = _player.VisualDescription
                };
            }
            
            builder.AppendLine("playerContext: ");
            builder.AppendLine(JsonSerializer.Serialize(playerToSerialize, options));
            builder.AppendLine();
        }
    }
} 