using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Strongbars.Tests.Utils;

namespace Strongbars.Tests;

public class FileGeneratorDiagnosticsTests
{
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
}
