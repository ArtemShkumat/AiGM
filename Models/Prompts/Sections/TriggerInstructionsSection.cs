using System.Text;

namespace AiGMBackEnd.Models.Prompts.Sections
{
    public class TriggerInstructionsSection : PromptSection
    {
        private readonly string _message;

        public TriggerInstructionsSection(string message = "This is being created based on a specific need in the game world.")
        {
            _message = message;
        }

        public override void AppendTo(StringBuilder builder)
        {
            builder.AppendLine("# Trigger Instructions");
            builder.AppendLine(_message);
            builder.AppendLine();
        }
    }
} 