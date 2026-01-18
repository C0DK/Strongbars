using System.Text.RegularExpressions;

namespace Strongbars.Abstractions;

public partial class TemplateRegex
{
    public static Regex ArgumentRegex = new Regex(
        @"\{\{\s*([a-zA-Z]\w*)\s*\}\}",
        RegexOptions.Compiled
    );
}
