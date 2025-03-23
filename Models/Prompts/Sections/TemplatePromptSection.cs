using System.Text;

namespace AiGMBackEnd.Models.Prompts.Sections
{
    public class TemplatePromptSection : PromptSection
    {
        private readonly string _title;
        private readonly string _content;
        private readonly string _prefix;
        
        public TemplatePromptSection(string title, string content, string prefix = "")
        {
            _title = title;
            _content = content;
            _prefix = prefix;
        }
        
        public override void AppendTo(StringBuilder builder)
        {
            if (!string.IsNullOrEmpty(_content))
            {
                if (!string.IsNullOrEmpty(_prefix))
                {
                    builder.AppendLine(_prefix);
                }
                
                builder.AppendLine($"# {_title}");
                builder.AppendLine(_content);
                builder.AppendLine();
            }
        }
    }
} 