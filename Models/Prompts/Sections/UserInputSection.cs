using System;
using System.Text;
using System.Text.Json;

namespace AiGMBackEnd.Models.Prompts.Sections
{
    public class UserInputSection : PromptSection
    {
        private readonly string _userInput;
        private readonly string _title;
        private readonly bool _detailed;

        public UserInputSection(string userInput, string title = "User Input", bool detailed = true)
        {
            _userInput = userInput;
            _title = title;
            _detailed = detailed;
        }

        public override void AppendTo(StringBuilder builder)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            
            object userInputObj;
            
            if (_detailed)
            {
                userInputObj = new
                {
                    title = _title,
                    content = _userInput,
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
            }
            else
            {
                userInputObj = _userInput;
            }
            
            builder.AppendLine("userInput: ");
            builder.AppendLine(JsonSerializer.Serialize(userInputObj, options));
            builder.AppendLine();
        }
    }
} 