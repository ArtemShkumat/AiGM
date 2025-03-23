using System.Text;

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
            builder.AppendLine($"# {_title}");
            builder.AppendLine(_userInput);
            builder.AppendLine();
        }
    }
} 