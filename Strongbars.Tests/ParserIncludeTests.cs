using Strongbars.Abstractions;
using Strongbars.Generator;

namespace Strongbars.Tests;

public class ParserIncludeTests
{
    private static ITemplateNode Parse(string content) => new Parser("test", content).Parse();

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
