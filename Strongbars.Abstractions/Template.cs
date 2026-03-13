using System.Diagnostics.CodeAnalysis;
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

    // Matches {% if varName %}...{% else %}...{% endif %} blocks.
    // Group 1: condition variable name, Group 2: then-content, Group 3: else-content (optional)
    public static Regex ConditionalRegex = new Regex(
        @"\{%\s*if\s+([a-zA-Z]\w*)\s*%\}([\s\S]*?)(?:\{%\s*else\s*%\}([\s\S]*?))?\{%\s*endif\s*%\}",
        RegexOptions.Compiled
    );

    // Matches {% unless varName %}...{% else %}...{% endunless %} blocks.
    // Group 1: condition variable name, Group 2: then-content, Group 3: else-content (optional)
    public static Regex UnlessRegex = new Regex(
        @"\{%\s*unless\s+([a-zA-Z]\w*)\s*%\}([\s\S]*?)(?:\{%\s*else\s*%\}([\s\S]*?))?\{%\s*endunless\s*%\}",
        RegexOptions.Compiled
    );
}

public abstract class TemplateArgument
{
    public abstract string Render();

    [return: NotNullIfNotNull("value")]
    public static implicit operator TemplateArgument?(string? value) =>
        value is null ? null : new StringArgument(value);

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
