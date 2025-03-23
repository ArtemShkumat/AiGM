using System.Text;

namespace AiGMBackEnd.Models.Prompts.Sections
{
    public abstract class PromptSection
    {
        public abstract void AppendTo(StringBuilder builder);
        
        public static void AppendSection(StringBuilder builder, string title, string content)
        {
            if (!string.IsNullOrEmpty(content))
            {
                builder.AppendLine($"# {title}");
                builder.AppendLine(content);
                builder.AppendLine();
            }
        }
    }
} 