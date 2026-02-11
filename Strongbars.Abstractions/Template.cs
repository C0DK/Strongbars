using System.Text.RegularExpressions;

namespace Strongbars.Abstractions;

public abstract partial class Template 
{
    public abstract string Render();

    public static implicit operator string(Template template) => template.Render();
    public override string ToString() => Render();

    public static Regex ArgumentRegex = new Regex(
        @"\{\{\s*(\.{2})?([a-zA-Z]\w*)(\?)?\s*\}\}",
        RegexOptions.Compiled
    );
}
