public abstract class TemplateArgument
{
    public abstract string Render();

    public static implicit operator string(TemplateArgument template) => template.Render();

    public static implicit operator TemplateArgument(string value) =>
        new StringTemplateArgument(value);

    public static implicit operator TemplateArgument(int value) =>
        new StringTemplateArgument(value.ToString());

    public static implicit operator TemplateArgument(decimal value) =>
        new StringTemplateArgument(value.ToString());

    public static implicit operator TemplateArgument(float value) =>
        new StringTemplateArgument(value.ToString());

    public static implicit operator TemplateArgument(double value) =>
        new StringTemplateArgument(value.ToString());

    public static implicit operator TemplateArgument(long value) =>
        new StringTemplateArgument(value.ToString());

    public override string ToString() => Render();
}

public class StringTemplateArgument : TemplateArgument
{
    private string content;

    public StringTemplateArgument(string content)
    {
        this.content = content;
    }

    public override string Render() => this.content;
}
