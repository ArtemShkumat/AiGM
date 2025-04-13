using System.Text;
using System.Text.Json;

namespace AiGMBackEnd.Models.Prompts.Sections
{
    public class GameSettingSection : PromptSection
    {
        private readonly GameSetting _gameSetting;
        private readonly bool _detailed;

        public GameSettingSection(GameSetting gameSetting, bool detailed = true)
        {
            _gameSetting = gameSetting;
            _detailed = detailed;
        }

        public override void AppendTo(StringBuilder builder)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            
            object gameSettingToSerialize = _gameSetting;
            
            if (!_detailed)
            {
                // Create a simplified game setting object with limited properties
                gameSettingToSerialize = new
                {
                    genre = _gameSetting.Genre,
                    theme = _gameSetting.Theme,
                    description = _gameSetting.Description,
                    setting = _gameSetting.Setting,
                    currencies = _gameSetting.Currencies,
                    economy = _gameSetting.Economy
                };
            }
            
            builder.AppendLine("gameSetting: ");
            builder.AppendLine(JsonSerializer.Serialize(gameSettingToSerialize, options));
            builder.AppendLine();
        }
    }
} 