using System.Text;

namespace AiGMBackEnd.Models.Prompts.Sections
{
    public class ConversationLogSection : PromptSection
    {
        private readonly ConversationLog _conversationLog;
        private readonly int _maxMessages;

        public ConversationLogSection(ConversationLog conversationLog, int maxMessages = 10)
        {
            _conversationLog = conversationLog;
            _maxMessages = maxMessages;
        }

        public override void AppendTo(StringBuilder builder)
        {
            builder.AppendLine("# Conversation History");

            // Just include the last N messages to keep the prompt size reasonable
            var recentMessages = _conversationLog.Messages
                .Skip(Math.Max(0, _conversationLog.Messages.Count - _maxMessages))
                .ToList();

            if (recentMessages.Count > 0)
            {
                foreach (var message in recentMessages)
                {
                    string sender = message.Sender == "user" ? "Player" : "DM";
                    builder.AppendLine($"{sender}: {message.Content}");
                }
            }
            else
            {
                builder.AppendLine("No previous conversation.");
            }
            builder.AppendLine();
        }
    }
} 