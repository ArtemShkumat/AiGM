using System.Text;
using System.Text.Json;

namespace AiGMBackEnd.Models.Prompts.Sections
{
    public class GamePreferencesSection : PromptSection
    {
        private readonly GamePreferences _gamePreferences;

        public GamePreferencesSection(GamePreferences gamePreferences)
        {
            _gamePreferences = gamePreferences;
        }

        public override void AppendTo(StringBuilder builder)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            
            builder.AppendLine("gamePreferences: ");
            builder.AppendLine(JsonSerializer.Serialize(_gamePreferences, options));
            builder.AppendLine();
        }
    }
} 