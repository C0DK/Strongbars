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
                new Variable("firstName", VariableType.TemplateArgument, false, false),
                new Variable("lastName", VariableType.TemplateArgument, false, false),
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
    public void ListHasConstructors()
    {
        Assert.That(
            typeof(List)
                .GetConstructors()
                .Select(con => con.GetParameters().Select(p => p.ParameterType).ToArray()),
            Is.EquivalentTo(
                new[]
                {
                    new[] { typeof(IEnumerable<string>) },
                    new[] { typeof(IEnumerable<TemplateArgument>) },
                }
            )
        );
    }

    [Test]
    public void ListHasConstructorsTest2()
    {
        Assert.That(
            typeof(TwoLists)
                .GetConstructors()
                .Select(con => con.GetParameters().Select(p => p.ParameterType).ToArray()),
            Is.EquivalentTo(
                new[]
                {
                    new[]
                    {
                        typeof(TemplateArgument),
                        typeof(IEnumerable<string>),
                        typeof(TemplateArgument),
                        typeof(IEnumerable<string>),
                    },
                    new[]
                    {
                        typeof(TemplateArgument),
                        typeof(IEnumerable<string>),
                        typeof(TemplateArgument),
                        typeof(IEnumerable<TemplateArgument>),
                    },
                    new[]
                    {
                        typeof(TemplateArgument),
                        typeof(IEnumerable<TemplateArgument>),
                        typeof(TemplateArgument),
                        typeof(IEnumerable<string>),
                    },
                    new[]
                    {
                        typeof(TemplateArgument),
                        typeof(IEnumerable<TemplateArgument>),
                        typeof(TemplateArgument),
                        typeof(IEnumerable<TemplateArgument>),
                    },
                }
            )
        );
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
    public void SupportIEnumerable()
    {
        var enumerable = Enumerable.Repeat(new ListItem("a"), 3);
        var template = new List(enumerable);

        Assert.That(
            template.Render(),
            Is.EqualTo("<ul><li>a</li><li>a</li><li>a</li></ul>").IgnoreWhiteSpace
        );
    }

    [Test]
    public void SupportIEnumerableOfString()
    {
        var enumerable = Enumerable.Repeat("a", 5);
        var template = new List(enumerable);

        Assert.That(template.Render(), Is.EqualTo("<ul>aaaaa</ul>").IgnoreWhiteSpace);
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
            Is.EquivalentTo([new Variable("items", VariableType.TemplateArgument, true, false)])
        );
    }

    [Test]
    public void SupportOptional()
    {
        Assert.That(
            TemplateWithOptional.Variables,
            Is.EquivalentTo([
                new Variable(
                    name: "items",
                    type: VariableType.TemplateArgument,
                    array: true,
                    optional: true
                ),
                new Variable(
                    name: "blah",
                    type: VariableType.TemplateArgument,
                    array: false,
                    optional: true
                ),
            ])
        );
    }

    [Test]
    public void OptionalIsOkayWithNullString()
    {
        string? nullable = null;
        Assert.That(new OnlyOptional(nullable).Render(), Is.EqualTo("").IgnoreWhiteSpace);
    }

    [Test]
    public void OptionalSample()
    {
        Assert.That(
            new TemplateWithOptional(null, (IEnumerable<string>?)null).Render(),
            Is.EqualTo("").IgnoreWhiteSpace
        );
        Assert.That(
            new TemplateWithOptional(blah: "a", items: (IEnumerable<string>?)null).Render(),
            Is.EqualTo("a").IgnoreWhiteSpace
        );
        Assert.That(
            new TemplateWithOptional(blah: null, items: (IEnumerable<string>?)[]).Render(),
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
