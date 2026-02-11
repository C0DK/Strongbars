using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Strongbars.Tests.Utils;

namespace Strongbars.Tests;

using Strongbars.Abstractions;
using Strongbars.Tests.Output;
using List = Strongbars.Tests.Output.List;

public class FileGeneratorTests
{
    [Test]
    public void ProducesExpectedFile_WithDefaults()
    {
        var globalOptions = TestAnalyzerConfigOptions.Empty;
        var textOptions = new Dictionary<AdditionalText, AnalyzerConfigOptions>
        {
            [new TestAdditionalText("Text1", "content1")] = new TestAnalyzerConfigOptions(
                new Dictionary<string, string>
                {
                    ["build_metadata.AdditionalFiles.StrongbarsNamespace"] = "fisk",
                }.ToImmutableDictionary()
            ),
            [new TestAdditionalText("Text2", "content2")] = TestAnalyzerConfigOptions.Empty,
        };

        var (diagnostics, output) = OutputGenerator.GetGeneratedOutput(globalOptions, textOptions);

        Assert.That(diagnostics, Is.Empty);
        Assert.That(output, Does.Contain("namespace fisk"));
        Assert.That(output, Does.Contain("public class"));
        Assert.That(output, Does.Contain("Text1"));
        Assert.That(output, Does.Contain("content1"));
        Assert.That(output, Does.Not.Contain("Text2"));
        Assert.That(output, Does.Not.Contain("content2"));
    }

    [Test]
    public void ProducesExpectedNamespaceAndVisibility_WithGlobalOptionsSet()
    {
        var globalOptions = new TestAnalyzerConfigOptions(
            new Dictionary<string, string>
            {
                ["build_property.StrongbarsVisibility"] = "internal",
            }.ToImmutableDictionary()
        );

        var textOptions = new Dictionary<AdditionalText, AnalyzerConfigOptions>
        {
            [new TestAdditionalText("Text1", "content1")] = new TestAnalyzerConfigOptions(
                new Dictionary<string, string>
                {
                    ["build_metadata.AdditionalFiles.StrongbarsNamespace"] = "fisk2",
                }.ToImmutableDictionary()
            ),
            [new TestAdditionalText("Text2", "content2")] = TestAnalyzerConfigOptions.Empty,
        };

        var (diagnostics, output) = OutputGenerator.GetGeneratedOutput(globalOptions, textOptions);

        Assert.That(diagnostics, Is.Empty);
        Assert.That(output, Does.Contain("namespace fisk2"));
        Assert.That(output, Does.Contain("internal class"));
        Assert.That(output, Does.Contain("Text1"));
        Assert.That(output, Does.Contain("content1"));
        Assert.That(output, Does.Not.Contain("Text2"));
        Assert.That(output, Does.Not.Contain("content2"));
    }

    [Test]
    public void HasExpectedArgs()
    {
        Assert.That(
            Name.Variables,
            Is.EquivalentTo([
                new Variable("firstName", VariableType.String, false),
                new Variable("lastName", VariableType.String, false),
            ])
        );
    }

    [Test]
    public void GeneratedClassGivesExpectedRender()
    {
        var template = new Name(firstName: "Bob", lastName: "Smith");

        Assert.That(template.Render(), Is.EqualTo("<p>Hello Bob Smith</p>").IgnoreWhiteSpace);
    }

    [Test]
    public void ToStringWorks()
    {
        var template = new Name(firstName: "Bobby", lastName: "Smith");

        Assert.That((string)template, Is.EqualTo("<p>Hello Bobby Smith</p>").IgnoreWhiteSpace);
    }

    [Test]
    public void IgnoresNewlinesInVariableBrackets()
    {
        var template = new Paragraph("Test");

        Assert.That(template.Render(), Is.EqualTo("<p>\n  Test\n</p>").IgnoreWhiteSpace);
    }

    [Test]
    public void ListExample()
    {
        var template = new Paragraph("Test");

        Assert.That(template.Render(), Is.EqualTo("<p>\n  Test\n</p>").IgnoreWhiteSpace);
    }

    [Test]
    public void ListSample()
    {
        var template = new List([new ListItem("alpha"), new ListItem("omega")]);

        Assert.That(
            template.Render(),
            Is.EqualTo("<ul><li>alpha</li><li>omega</li></ul>").IgnoreWhiteSpace
        );
    }

    [Test]
    public void ListSampleParams()
    {
        var template = new List(new ListItem("alpha"), new ListItem("omega"));

        Assert.That(
            template.Render(),
            Is.EqualTo("<ul><li>alpha</li><li>omega</li></ul>").IgnoreWhiteSpace
        );
    }


    [Test]
    public void SupportArray()
    {
        Assert.That(
            List.Variables,
            Is.EquivalentTo([new Variable("items", VariableType.Array, false)])
        );
    }

    [Test]
    public void SupportOptional()
    {
        Assert.That(
            TemplateWithOptional.Variables,
            Is.EquivalentTo([
                new Variable("items", VariableType.Array, true),
                new Variable("blah", VariableType.String, true),
            ])
        );
    }

    [Test]
    public void OptionalSample()
    {
        Assert.That(new TemplateWithOptional(null, null).Render(), Is.EqualTo("").IgnoreWhiteSpace);
        Assert.That(
            new TemplateWithOptional(blah: "a", items: null).Render(),
            Is.EqualTo("a").IgnoreWhiteSpace
        );
        Assert.That(
            new TemplateWithOptional(blah: null, items: []).Render(),
            Is.EqualTo("").IgnoreWhiteSpace
        );
        Assert.That(
            new TemplateWithOptional(blah: null, items: ["a", "b", "cde"]).Render(),
            Is.EqualTo("abcde").IgnoreWhiteSpace
        );
    }

    [Test]
    public void BrokenHasNoArgs()
    {
        Assert.That(Broken.Variables, Is.Empty);
    }
}
