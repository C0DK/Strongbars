using Strongbars.Generator;

namespace Strongbars.Tests;

public class ParserErrorTests
{
    private static void Parse(string content) => new Parser("test", content).Parse();

    [Test]
    public void InvalidVariableName_ThrowsParserError()
    {
        var ex = Assert.Throws<ParserError>(() => Parse("{{ 123bad }}"));

        Assert.That(ex!.Reason, Does.Contain("not a valid variable name"));
        Assert.That(ex.TemplateName, Is.EqualTo("test"));
    }

    [Test]
    public void InvalidExpression_ThrowsParserError()
    {
        var ex = Assert.Throws<ParserError>(() => Parse("{% notakeyword %}"));

        Assert.That(ex!.Reason, Does.Contain("not a valid expression"));
    }

    [Test]
    public void UnclosedConditional_ThrowsParserError()
    {
        var ex = Assert.Throws<ParserError>(() => Parse("{% if x %}body without end"));

        Assert.That(ex!.Reason, Does.Contain("Conditional doesnt end"));
    }

    [Test]
    public void ParserError_IncludesMatchPosition()
    {
        var ex = Assert.Throws<ParserError>(() => Parse("hello {{ 123bad }} world"));

        Assert.That(ex!.Match.Index, Is.EqualTo(6)); // position of "{{ 123bad }}"
        Assert.That(ex.Match.Length, Is.GreaterThan(0));
    }
}
