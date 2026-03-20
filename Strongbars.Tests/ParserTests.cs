using Strongbars.Abstractions;
using Strongbars.Generator;

namespace Strongbars.Tests;

/// <summary>
/// Unit tests for <see cref="Parser"/> in isolation, verifying the AST it builds
/// and that <see cref="ParserError"/> is thrown for invalid templates.
/// </summary>
public class ParserTests
{
    private static ITemplateNode Parse(string content) => new Parser("test", content).Parse();

    // ── Literal-only templates ────────────────────────────────────────────────

    [Test]
    public void PlainText_ReturnsSingleLiteralNode()
    {
        var node = Parse("hello world");

        Assert.That(node, Is.InstanceOf<LiteralTemplateNode>());
        Assert.That(((LiteralTemplateNode)node).Content, Is.EqualTo("hello world"));
    }

    [Test]
    public void EmptyTemplate_ReturnsSingleLiteralNode()
    {
        var node = Parse("");

        Assert.That(node, Is.InstanceOf<LiteralTemplateNode>());
        Assert.That(((LiteralTemplateNode)node).Content, Is.EqualTo(""));
    }

    // ── Variable nodes ────────────────────────────────────────────────────────

    [Test]
    public void SingleVariable_ReturnsCompositeWithVariableNode()
    {
        var node = (CompositeTemplateNode)Parse("{{name}}");
        var variable = (VariableTemplateNode)node.Nodes.Single();

        Assert.That(variable.Variable.Name, Is.EqualTo("name"));
        Assert.That(variable.Variable.Array, Is.False);
        Assert.That(variable.Variable.Optional, Is.False);
    }

    [Test]
    public void OptionalVariable_SetsOptionalFlag()
    {
        var node = (CompositeTemplateNode)Parse("{{foo?}}");
        var variable = (VariableTemplateNode)node.Nodes.Single();

        Assert.That(variable.Variable.Name, Is.EqualTo("foo"));
        Assert.That(variable.Variable.Optional, Is.True);
    }

    [Test]
    public void ArrayVariable_SetsArrayFlag()
    {
        var node = (CompositeTemplateNode)Parse("{{..items}}");
        var variable = (VariableTemplateNode)node.Nodes.Single();

        Assert.That(variable.Variable.Name, Is.EqualTo("items"));
        Assert.That(variable.Variable.Array, Is.True);
    }

    [Test]
    public void VariableWithSurroundingLiterals_ProducesThreeNodes()
    {
        var node = (CompositeTemplateNode)Parse("Hello {{name}}!");

        Assert.That(node.Nodes, Has.Count.EqualTo(3));
        Assert.That(node.Nodes[0], Is.InstanceOf<LiteralTemplateNode>());
        Assert.That(node.Nodes[1], Is.InstanceOf<VariableTemplateNode>());
        Assert.That(node.Nodes[2], Is.InstanceOf<LiteralTemplateNode>());
    }

    [Test]
    public void MultipleVariables_AllParsed()
    {
        var node = (CompositeTemplateNode)Parse("{{first}} {{second}}");
        var variables = node.Nodes.OfType<VariableTemplateNode>().ToArray();

        Assert.That(variables, Has.Length.EqualTo(2));
        Assert.That(variables[0].Variable.Name, Is.EqualTo("first"));
        Assert.That(variables[1].Variable.Name, Is.EqualTo("second"));
    }

    // ── Conditional nodes ─────────────────────────────────────────────────────

    [Test]
    public void IfBlock_ProducesConditionalNode()
    {
        var node = (CompositeTemplateNode)Parse("{% if active %}yes{% end %}");
        var cond = (ConditionalTemplateNode)node.Nodes.Single();

        Assert.That(cond.Conditional.Name, Is.EqualTo("active"));
        Assert.That(cond.Conditional.Type, Is.EqualTo(VariableType.Bool));
    }

    [Test]
    public void IfBlock_IfTrueContainsLiteral()
    {
        var node = (CompositeTemplateNode)Parse("{% if active %}yes{% end %}");
        var cond = (ConditionalTemplateNode)node.Nodes.Single();
        var ifTrue = (CompositeTemplateNode)cond.IfTrue;

        Assert.That(ifTrue.Nodes.OfType<LiteralTemplateNode>().Single().Content, Is.EqualTo("yes"));
    }

    [Test]
    public void IfBlock_IfFalseIsEmptyComposite()
    {
        var node = (CompositeTemplateNode)Parse("{% if active %}yes{% end %}");
        var cond = (ConditionalTemplateNode)node.Nodes.Single();
        var ifFalse = (CompositeTemplateNode)cond.IfFalse;

        Assert.That(ifFalse.Nodes, Is.Empty);
    }

    [Test]
    public void IfElseBlock_BothBranchesPopulated()
    {
        var node = (CompositeTemplateNode)Parse("{% if enabled %}on{% else %}off{% end %}");
        var cond = (ConditionalTemplateNode)node.Nodes.Single();

        var ifTrue = (CompositeTemplateNode)cond.IfTrue;
        Assert.That(ifTrue.Nodes.OfType<LiteralTemplateNode>().Single().Content, Is.EqualTo("on"));

        var ifFalse = (CompositeTemplateNode)cond.IfFalse;
        Assert.That(
            ifFalse.Nodes.OfType<LiteralTemplateNode>().Single().Content,
            Is.EqualTo("off")
        );
    }

    [Test]
    public void UnlessBlock_InvertsIfTrueAndIfFalse()
    {
        // {% unless x %}body{% end %}: condition=x, IfTrue="" (shown when x=true), IfFalse="body"
        var node = (CompositeTemplateNode)Parse("{% unless x %}body{% end %}");
        var cond = (ConditionalTemplateNode)node.Nodes.Single();

        // IfTrue (rendered when condition is true) is empty for "unless"
        var ifTrue = (CompositeTemplateNode)cond.IfTrue;
        Assert.That(ifTrue.Nodes, Is.Empty);

        // IfFalse (rendered when condition is false) contains the body
        var ifFalse = (CompositeTemplateNode)cond.IfFalse;
        Assert.That(
            ifFalse.Nodes.OfType<LiteralTemplateNode>().Single().Content,
            Is.EqualTo("body")
        );
    }

    // ── GetVariables ──────────────────────────────────────────────────────────

    [Test]
    public void GetVariables_ReturnsAllVariablesIncludingConditionals()
    {
        var node = Parse("{% if active %}{{message}}{% end %}");
        var names = node.GetVariables().Select(v => v.Name).ToArray();

        Assert.That(names, Is.EquivalentTo(new[] { "active", "message" }));
    }

    // ── Error cases ───────────────────────────────────────────────────────────

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

    // ── Include directives ────────────────────────────────────────────────────

    private static ITemplateNode ParseWithIncludes(
        string content,
        string fileDirectory,
        Dictionary<string, string> files
    ) =>
        new Parser(
            "test",
            content,
            fileDirectory: fileDirectory,
            fileReader: path => files.TryGetValue(path, out var c) ? c : null
        ).Parse();

    [Test]
    public void Include_InlinesIncludedTemplateContent()
    {
        var files = new Dictionary<string, string>
        {
            ["/root/partial.html"] = "hello world",
        };

        var node = ParseWithIncludes("{% include partial.html %}", "/root", files);

        Assert.That(node, Is.InstanceOf<LiteralTemplateNode>());
        Assert.That(((LiteralTemplateNode)node).Content, Is.EqualTo("hello world"));
    }

    [Test]
    public void Include_RelativePath_ResolvesFromFileDirectory()
    {
        var files = new Dictionary<string, string>
        {
            ["/root/partials/header.html"] = "HEADER",
        };

        var node = ParseWithIncludes("{% include partials/header.html %}", "/root", files);

        Assert.That(node, Is.InstanceOf<LiteralTemplateNode>());
        Assert.That(((LiteralTemplateNode)node).Content, Is.EqualTo("HEADER"));
    }

    [Test]
    public void Include_DotDotPath_ResolvesFromFileDirectory()
    {
        var files = new Dictionary<string, string>
        {
            ["/root/shared.html"] = "SHARED",
        };

        var node = ParseWithIncludes(
            "{% include ../shared.html %}",
            "/root/subdir",
            files
        );

        Assert.That(node, Is.InstanceOf<LiteralTemplateNode>());
        Assert.That(((LiteralTemplateNode)node).Content, Is.EqualTo("SHARED"));
    }

    [Test]
    public void Include_AbsolutePath_ResolvesFromProjectRoot()
    {
        var files = new Dictionary<string, string>
        {
            ["/projectroot/partials/nav.html"] = "NAV",
        };

        var node = new Parser(
            "test",
            "{% include /partials/nav.html %}",
            fileDirectory: "/projectroot/pages",
            projectRoot: "/projectroot",
            fileReader: path => files.TryGetValue(path, out var c) ? c : null
        ).Parse();

        Assert.That(node, Is.InstanceOf<LiteralTemplateNode>());
        Assert.That(((LiteralTemplateNode)node).Content, Is.EqualTo("NAV"));
    }

    [Test]
    public void Include_IncludedVariablesAreExposedInGetVariables()
    {
        var files = new Dictionary<string, string>
        {
            ["/root/partial.html"] = "{{name}}",
        };

        var node = ParseWithIncludes("{% include partial.html %}", "/root", files);
        var variables = node.GetVariables().ToArray();

        Assert.That(variables, Has.Length.EqualTo(1));
        Assert.That(variables[0].Name, Is.EqualTo("name"));
    }

    [Test]
    public void Include_InsideConditional_Works()
    {
        var files = new Dictionary<string, string>
        {
            ["/root/body.html"] = "BODY",
        };

        var node = ParseWithIncludes(
            "{% if active %}{% include body.html %}{% end %}",
            "/root",
            files
        );

        var composite = (CompositeTemplateNode)node;
        var cond = (ConditionalTemplateNode)composite.Nodes.Single();
        var ifTrue = (CompositeTemplateNode)cond.IfTrue;
        Assert.That(ifTrue.Nodes, Has.Count.GreaterThan(0));
    }

    [Test]
    public void Include_CircularInclude_ThrowsParserError()
    {
        var files = new Dictionary<string, string>
        {
            ["/root/a.html"] = "{% include b.html %}",
            ["/root/b.html"] = "{% include a.html %}",
        };

        var ex = Assert.Throws<ParserError>(
            () =>
                new Parser(
                    "/root/a.html",
                    "{% include a.html %}",
                    fileDirectory: "/root",
                    fileReader: path => files.TryGetValue(path, out var c) ? c : null,
                    includedPaths: new HashSet<string> { "/root/a.html" }
                ).Parse()
        );

        Assert.That(ex!.Reason, Does.Contain("Circular include"));
    }

    [Test]
    public void Include_FileNotFound_ThrowsParserError()
    {
        var ex = Assert.Throws<ParserError>(
            () =>
                new Parser(
                    "test",
                    "{% include missing.html %}",
                    fileDirectory: "/root",
                    fileReader: _ => null
                ).Parse()
        );

        Assert.That(ex!.Reason, Does.Contain("Include file not found"));
    }

    [Test]
    public void Include_WithoutFileDirectory_ThrowsParserError()
    {
        var ex = Assert.Throws<ParserError>(() => Parse("{% include something.html %}"));

        Assert.That(ex!.Reason, Does.Contain("file directory is unavailable"));
    }
}
