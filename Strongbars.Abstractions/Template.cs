using System.Diagnostics.CodeAnalysis;

namespace Strongbars.Abstractions;

public abstract partial class Template : TemplateArgument
{
    public static implicit operator string(Template template) => template.Render();
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
