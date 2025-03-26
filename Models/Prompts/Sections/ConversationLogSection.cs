using System.Text;
using System.Text.Json;

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
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            // Just include the last N messages to keep the prompt size reasonable
            var recentMessages = _conversationLog.Messages
                .Skip(Math.Max(0, _conversationLog.Messages.Count - _maxMessages))
                .ToList();

            var conversationLogToSerialize = new ConversationLog
            {
                Messages = recentMessages
            };

            builder.AppendLine("conversationLog: ");
            builder.AppendLine(JsonSerializer.Serialize(conversationLogToSerialize, options));
            builder.AppendLine();
        }
    }
} 