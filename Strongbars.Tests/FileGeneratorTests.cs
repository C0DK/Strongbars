using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Strongbars.Tests.Utils;

namespace Strongbars.Tests;

public class FileGeneratorConfigurationTests
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
}
