using System.Text;
using System.Text.Json;

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
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            
            builder.AppendLine("gameSetting: ");
            builder.AppendLine(JsonSerializer.Serialize(_gameSetting, options));
            builder.AppendLine();
        }
    }
} 