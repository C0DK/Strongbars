using System.Collections.Immutable;
using System.IO;
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
    public void SubdirectoryAppendsToNamespace()
    {
        var textOptions = new Dictionary<AdditionalText, AnalyzerConfigOptions>
        {
            [new TestAdditionalText("Bar", "content")] = new TestAnalyzerConfigOptions(
                new Dictionary<string, string>
                {
                    ["build_metadata.AdditionalFiles.StrongbarsNamespace"] = "Pages",
                    ["build_metadata.AdditionalFiles.RecursiveDir"] = "Foo/",
                }.ToImmutableDictionary()
            ),
        };

        var (diagnostics, output) = OutputGenerator.GetGeneratedOutput(
            TestAnalyzerConfigOptions.Empty,
            textOptions
        );

        Assert.That(diagnostics, Is.Empty);
        Assert.That(output, Does.Contain("namespace Pages.Foo"));
        Assert.That(output, Does.Contain("class Bar"));
    }

    [Test]
    public void NestedSubdirectoryAppendsAllSegmentsToNamespace()
    {
        var textOptions = new Dictionary<AdditionalText, AnalyzerConfigOptions>
        {
            [new TestAdditionalText("Baz", "content")] = new TestAnalyzerConfigOptions(
                new Dictionary<string, string>
                {
                    ["build_metadata.AdditionalFiles.StrongbarsNamespace"] = "Pages",
                    ["build_metadata.AdditionalFiles.RecursiveDir"] = "Foo/Bar/",
                }.ToImmutableDictionary()
            ),
        };

        var (diagnostics, output) = OutputGenerator.GetGeneratedOutput(
            TestAnalyzerConfigOptions.Empty,
            textOptions
        );

        Assert.That(diagnostics, Is.Empty);
        Assert.That(output, Does.Contain("namespace Pages.Foo.Bar"));
    }

    [Test]
    public void NoSubdirectoryLeavesNamespaceUnchanged()
    {
        var textOptions = new Dictionary<AdditionalText, AnalyzerConfigOptions>
        {
            [new TestAdditionalText("Toast", "content")] = new TestAnalyzerConfigOptions(
                new Dictionary<string, string>
                {
                    ["build_metadata.AdditionalFiles.StrongbarsNamespace"] = "Pages",
                }.ToImmutableDictionary()
            ),
        };

        var (diagnostics, output) = OutputGenerator.GetGeneratedOutput(
            TestAnalyzerConfigOptions.Empty,
            textOptions
        );

        Assert.That(diagnostics, Is.Empty);
        Assert.That(output, Does.Contain("namespace Pages"));
        Assert.That(output, Does.Not.Contain("namespace Pages."));
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
    public void ConditionalVariablesHaveCorrectMetadata()
    {
        Assert.That(
            Message.Variables,
            Is.EquivalentTo([
                new Variable("urgent", VariableType.Bool, array: false, optional: false),
                new Variable(
                    "message",
                    VariableType.TemplateArgument,
                    array: false,
                    optional: false
                ),
            ])
        );
    }

    [Test]
    public void ConditionalRendersContentWhenTrue()
    {
        var template = new Message(urgent: true, message: "Hello!");

        Assert.That(template.Render(), Does.Contain("urgent"));
        Assert.That(template.Render(), Does.Contain("Hello!"));
    }

    [Test]
    public void ConditionalRendersEmptyWhenFalse()
    {
        var template = new Message(urgent: false, message: "Hello!");

        Assert.That(template.Render(), Does.Not.Contain("urgent"));
        Assert.That(template.Render(), Does.Contain("Hello!"));
    }

    [Test]
    public void ConditionalFullRenderMatchesExpected()
    {
        Assert.That(
            new Message(urgent: true, message: "Buy now!").Render(),
            Is.EqualTo(@"<div class=""message urgent"">Buy now!</div>").IgnoreWhiteSpace
        );
        Assert.That(
            new Message(urgent: false, message: "Buy now!").Render(),
            Is.EqualTo(@"<div class=""message "">Buy now!</div>").IgnoreWhiteSpace
        );
    }

    [Test]
    public void UnlessVariablesHaveCorrectMetadata()
    {
        Assert.That(
            Status.Variables,
            Is.EquivalentTo([
                new Variable("inactive", VariableType.Bool, array: false, optional: false),
                new Variable("label", VariableType.TemplateArgument, array: false, optional: false),
            ])
        );
    }

    [Test]
    public void UnlessRendersContentWhenFalse()
    {
        var template = new Status(inactive: false, label: "Online");

        Assert.That(template.Render(), Does.Contain("active"));
        Assert.That(template.Render(), Does.Contain("Online"));
    }

    [Test]
    public void UnlessRendersEmptyWhenTrue()
    {
        var template = new Status(inactive: true, label: "Offline");

        Assert.That(template.Render(), Does.Not.Contain("active"));
        Assert.That(template.Render(), Does.Contain("Offline"));
    }

    [Test]
    public void UnlessFullRenderMatchesExpected()
    {
        Assert.That(
            new Status(inactive: false, label: "Online").Render(),
            Is.EqualTo(@"<span class=""status active"">Online</span>").IgnoreWhiteSpace
        );
        Assert.That(
            new Status(inactive: true, label: "Offline").Render(),
            Is.EqualTo(@"<span class=""status "">Offline</span>").IgnoreWhiteSpace
        );
    }

    [Test]
    public void IfElseRendersElseBranchWhenFalse()
    {
        Assert.That(
            new Toggle(enabled: true, label: "Go").Render(),
            Is.EqualTo(@"<button class=""enabled"">Go</button>").IgnoreWhiteSpace
        );
        Assert.That(
            new Toggle(enabled: false, label: "Go").Render(),
            Is.EqualTo(@"<button class=""disabled"">Go</button>").IgnoreWhiteSpace
        );
    }

    [Test]
    public void UnlessElseRendersElseBranchWhenTrue()
    {
        Assert.That(
            new Subscription(premium: false).Render(),
            Is.EqualTo("<p>Free tier</p>").IgnoreWhiteSpace
        );
        Assert.That(
            new Subscription(premium: true).Render(),
            Is.EqualTo("<p>Premium member</p>").IgnoreWhiteSpace
        );
    }

    [Test]
    public void ElseBranchDoesNotAffectTemplatesWithoutElse()
    {
        // Existing templates without {% else %} must still behave identically
        Assert.That(
            new Message(urgent: false, message: "All good").Render(),
            Is.EqualTo(@"<div class=""message "">All good</div>").IgnoreWhiteSpace
        );
        Assert.That(
            new Status(inactive: true, label: "Offline").Render(),
            Is.EqualTo(@"<span class=""status "">Offline</span>").IgnoreWhiteSpace
        );
    }

    [Test]
    public void IncludeDirective_InlinesIncludedFileVariables()
    {
        Assert.That(
            GreetingWithInclude.Variables,
            Is.EquivalentTo([
                new Variable("name", VariableType.TemplateArgument, array: false, optional: false),
            ])
        );
    }

    [Test]
    public void IncludeDirective_RendersIncludedContent()
    {
        Assert.That(
            new GreetingWithInclude(name: "World").Render(),
            Is.EqualTo("Hello <b>World</b>!").IgnoreWhiteSpace
        );
    }

    [Test]
    public void IncludeDirective_ViaOutputGenerator_InlinesContent()
    {
        var tempDir = Path.GetTempPath();
        var partialPath = Path.Combine(tempDir, $"partial_{Guid.NewGuid():N}.html");
        File.WriteAllText(partialPath, "<span>{{city}}</span>");
        var mainPath = Path.Combine(tempDir, $"main_{Guid.NewGuid():N}.html");
        try
        {
            var textOptions = new Dictionary<AdditionalText, AnalyzerConfigOptions>
            {
                [new TestAdditionalText(
                    mainPath,
                    $"{{% include {Path.GetFileName(partialPath)} %}}"
                )] = new TestAnalyzerConfigOptions(
                    new Dictionary<string, string>
                    {
                        ["build_metadata.AdditionalFiles.StrongbarsNamespace"] = "TestNs",
                    }.ToImmutableDictionary()
                ),
            };

            var (diagnostics, source) = OutputGenerator.GetGeneratedOutput(
                TestAnalyzerConfigOptions.Empty,
                textOptions
            );

            Assert.That(diagnostics, Is.Empty);
            Assert.That(source, Is.Not.Null);
            Assert.That(source, Does.Contain("TemplateArgument city"));
        }
        finally
        {
            File.Delete(partialPath);
        }
    }

    [Test]
    public void IncludeDirective_MissingFile_ProducesSB003Diagnostic()
    {
        var (diagnostics, _) = GenerateFromTemplate(
            "Bad",
            "{% include /missing/file.html %}"
        );

        Assert.That(diagnostics, Has.One.Matches<Diagnostic>(d => d.Id == "SB003"));
    }

    private static (ImmutableArray<Diagnostic>, string?) GenerateFromTemplate(
        string templateName,
        string templateContent
    )
    {
        var textOptions = new Dictionary<AdditionalText, AnalyzerConfigOptions>
        {
            [new TestAdditionalText(templateName, templateContent)] = new TestAnalyzerConfigOptions(
                new Dictionary<string, string>
                {
                    ["build_metadata.AdditionalFiles.StrongbarsNamespace"] = "TestNs",
                }.ToImmutableDictionary()
            ),
        };
        return OutputGenerator.GetGeneratedOutput(TestAnalyzerConfigOptions.Empty, textOptions);
    }

    [Test]
    public void InvalidVariableNameProducesSB003Diagnostic()
    {
        var (diagnostics, _) = GenerateFromTemplate("Bad", "{{ 123invalid }}");

        Assert.That(diagnostics, Has.One.Matches<Diagnostic>(d => d.Id == "SB003"));
        Assert.That(
            diagnostics.Single(d => d.Id == "SB003").GetMessage(),
            Does.Contain("not a valid variable name")
        );
    }

    [Test]
    public void InvalidExpressionProducesSB003Diagnostic()
    {
        var (diagnostics, _) = GenerateFromTemplate("Bad", "{% badkeyword %}");

        Assert.That(diagnostics, Has.One.Matches<Diagnostic>(d => d.Id == "SB003"));
        Assert.That(
            diagnostics.Single(d => d.Id == "SB003").GetMessage(),
            Does.Contain("not a valid expression")
        );
    }

    [Test]
    public void UnclosedConditionalProducesSB003Diagnostic()
    {
        var (diagnostics, _) = GenerateFromTemplate("Bad", "{% if foo %}no closing end tag");

        Assert.That(diagnostics, Has.One.Matches<Diagnostic>(d => d.Id == "SB003"));
        Assert.That(
            diagnostics.Single(d => d.Id == "SB003").GetMessage(),
            Does.Contain("Conditional doesnt end")
        );
    }

    [Test]
    public void SB003DiagnosticIsError()
    {
        var (diagnostics, _) = GenerateFromTemplate("Bad", "{{ 123invalid }}");

        Assert.That(
            diagnostics.Single(d => d.Id == "SB003").Severity,
            Is.EqualTo(DiagnosticSeverity.Error)
        );
    }

    [Test]
    public void SameVariableUsedTwiceGeneratesSingleConstructorParameter()
    {
        var (diagnostics, source) = GenerateFromTemplate(
            "Tmpl",
            "Hello {{name}}, your name is {{name}} again."
        );

        Assert.That(diagnostics, Is.Empty);
        Assert.That(source, Is.Not.Null);
        // Constructor must not have duplicate `name` parameters
        Assert.That(source, Does.Not.Match(@"TemplateArgument name,\s*TemplateArgument name"));
    }

    [Test]
    public void RequiredVariableOverridesOptionalWhenSameNameUsedBoth()
    {
        // {{name}} is required, {{name?}} is optional — result should be required (no ?)
        var (diagnostics, source) = GenerateFromTemplate("Tmpl", "{{name}} and {{name?}}");

        Assert.That(diagnostics, Is.Empty);
        Assert.That(source, Is.Not.Null);
        // The field should be non-nullable (TemplateArgument, not TemplateArgument?)
        Assert.That(source, Does.Contain("TemplateArgument name"));
        Assert.That(source, Does.Not.Contain("TemplateArgument? name"));
    }

    [Test]
    public void TypeConflictBetweenBoolAndTemplateArgumentProducesSB003Diagnostic()
    {
        // {% if items %} treats `items` as Bool; {{..items}} treats it as TemplateArgument array
        var (diagnostics, _) = GenerateFromTemplate(
            "Tmpl",
            "{% if items %}yes{% end %}{{..items}}"
        );

        Assert.That(diagnostics, Has.One.Matches<Diagnostic>(d => d.Id == "SB003"));
        Assert.That(
            diagnostics.Single(d => d.Id == "SB003").GetMessage(),
            Does.Contain("conflicting types")
        );
    }
}
