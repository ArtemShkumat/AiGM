using System.Text;
using System.Text.Json;

namespace AiGMBackEnd.Models.Prompts.Sections
{
    public class UserInputSection : PromptSection
    {
        private readonly string _userInput;
        private readonly string _title;

        public UserInputSection(string userInput, string title = "User Input")
        {
            _userInput = userInput;
            _title = title;
        }

        public override void AppendTo(StringBuilder builder)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            
            var userInputObj = new
            {
                title = _title,
                content = _userInput
            };
            
            builder.AppendLine("userInput: ");
            builder.AppendLine(JsonSerializer.Serialize(userInputObj, options));
            builder.AppendLine();
        }
    }
} 