using System.Text.RegularExpressions;

namespace Strongbars.Abstractions;

public abstract partial class Template : TemplateArgument
{
    public static implicit operator string(Template template) => template.Render();

    // TODO: also handle formattable.
    public static Regex ArgumentRegex = new Regex(
        @"\{\{\s*(\.{2})?([a-zA-Z]\w*)(\?)?\s*\}\}",
        RegexOptions.Compiled
    );
}

public abstract class TemplateArgument
{
    public abstract string Render();

    public static implicit operator TemplateArgument(string value) => new StringArgument(value);

    public static implicit operator TemplateArgument(int value) =>
        new StringArgument(value.ToString());

    public override string ToString() => Render();
}

public class StringArgument : TemplateArgument
{
    private readonly string content;

    public StringArgument(string content)
    {
        this.content = content;
    }

    public override string Render() => content;
}
