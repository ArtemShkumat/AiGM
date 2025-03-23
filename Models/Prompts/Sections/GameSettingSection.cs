using System.Text;

namespace AiGMBackEnd.Models.Prompts.Sections
{
    public class GameSettingSection : PromptSection
    {
        private readonly GameSetting _gameSetting;

        public GameSettingSection(GameSetting gameSetting)
        {
            _gameSetting = gameSetting;
        }

        public override void AppendTo(StringBuilder builder)
        {
            builder.AppendLine("# Game Setting");
            builder.AppendLine($"Genre: {_gameSetting.Genre}");
            builder.AppendLine($"Theme: {_gameSetting.Theme}");
            builder.AppendLine($"Description: {_gameSetting.Description}");
            
            if (!string.IsNullOrEmpty(_gameSetting.Setting))
            {
                builder.AppendLine($"Setting: {_gameSetting.Setting}");
            }
            
            if (!string.IsNullOrEmpty(_gameSetting.StartingLocation))
            {
                builder.AppendLine($"Starting location: {_gameSetting.StartingLocation}");
            }
            
            builder.AppendLine();
        }
    }
} 