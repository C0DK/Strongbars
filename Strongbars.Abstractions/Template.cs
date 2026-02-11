using System.Text.RegularExpressions;

namespace Strongbars.Abstractions;

public abstract partial class Template : TemplateArgument
{
    public static Regex ArgumentRegex = new Regex(
        @"\{\{\s*(\.{2})?([a-zA-Z]\w*)(\?)?\s*\}\}",
        RegexOptions.Compiled
    );
}
