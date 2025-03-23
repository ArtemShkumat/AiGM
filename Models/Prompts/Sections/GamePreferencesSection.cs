using System.Text;

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
            builder.AppendLine("# Game Preferences");
            builder.AppendLine($"Tone: {_gamePreferences.Tone}");
            builder.AppendLine($"Complexity: {_gamePreferences.Complexity}");
            builder.AppendLine($"Age Appropriateness: {_gamePreferences.AgeAppropriateness}");
            builder.AppendLine();
        }
    }
} 