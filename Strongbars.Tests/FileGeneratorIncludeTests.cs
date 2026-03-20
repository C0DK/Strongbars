using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Strongbars.Abstractions;
using Strongbars.Tests.Output;
using Strongbars.Tests.Utils;

namespace Strongbars.Tests;

public class FileGeneratorIncludeTests
{
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
                [
                    new TestAdditionalText(
                        mainPath,
                        $"{{% include {Path.GetFileName(partialPath)} %}}"
                    )
                ] = new TestAnalyzerConfigOptions(
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
        var (diagnostics, _) = GenerateFromTemplate("Bad", "{% include /missing/file.html %}");

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
}
