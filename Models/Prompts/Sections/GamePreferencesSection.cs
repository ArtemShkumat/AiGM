using System.Text;
using System.Text.Json;

namespace AiGMBackEnd.Models.Prompts.Sections
{
    public class GamePreferencesSection : PromptSection
    {
        private readonly GamePreferences _gamePreferences;
        private readonly bool _detailed;

        public GamePreferencesSection(GamePreferences gamePreferences, bool detailed = true)
        {
            _gamePreferences = gamePreferences;
            _detailed = detailed;
        }

        public override void AppendTo(StringBuilder builder)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            
            object gamePreferencesToSerialize = _gamePreferences;
            
            if (!_detailed)
            {
                // Create a simplified game preferences object with limited properties
                gamePreferencesToSerialize = new
                {
                    tone = _gamePreferences.Tone,
                    complexity = _gamePreferences.Complexity
                };
            }
            
            builder.AppendLine("gamePreferences: ");
            builder.AppendLine(JsonSerializer.Serialize(gamePreferencesToSerialize, options));
            builder.AppendLine();
        }
    }
} 