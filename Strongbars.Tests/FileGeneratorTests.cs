using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Strongbars.Tests.Utils;

namespace Strongbars.Tests;

using Strongbars.Out;

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
                    ["build_metadata.AdditionalFiles.Strongbars"] = "Text1",
                }.ToImmutableDictionary()
            ),
            [new TestAdditionalText("Text2", "content2")] = TestAnalyzerConfigOptions.Empty,
        };

        var (diagnostics, output) = OutputGenerator.GetGeneratedOutput(globalOptions, textOptions);

        Assert.That(diagnostics, Is.Empty);
        Assert.That(output, Does.Contain("namespace Strongbars.Out"));
        Assert.That(output, Does.Contain("internal class"));
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
                ["build_property.StrongbarsNamespace"] = "Different.Namespace",
                ["build_property.StrongbarsVisibility"] = "public",
            }.ToImmutableDictionary()
        );

        var textOptions = new Dictionary<AdditionalText, AnalyzerConfigOptions>
        {
            [new TestAdditionalText("Text1", "content1")] = new TestAnalyzerConfigOptions(
                new Dictionary<string, string>
                {
                    ["build_metadata.AdditionalFiles.Strongbars"] = "Text1",
                }.ToImmutableDictionary()
            ),
            [new TestAdditionalText("Text2", "content2")] = TestAnalyzerConfigOptions.Empty,
        };

        var (diagnostics, output) = OutputGenerator.GetGeneratedOutput(globalOptions, textOptions);

        Assert.That(diagnostics, Is.Empty);
        Assert.That(output, Does.Contain("namespace Different.Namespace"));
        Assert.That(output, Does.Contain("public class"));
        Assert.That(output, Does.Contain("Text1"));
        Assert.That(output, Does.Contain("content1"));
        Assert.That(output, Does.Not.Contain("Text2"));
        Assert.That(output, Does.Not.Contain("content2"));
    }
    [Test]
    public void HasExpectedArgs()
    {
        Assert.That(Name.Arguments, Is.EquivalentTo(["firstName", "lastName"]));
    }

    [Test]
    public void GeneratedClassGivesExpectedRender()
    {
        var template = new Name(firstName: "Bob", lastName: "Smith");

        Assert.That(template.Render().Trim(), Is.EqualTo("<p>Hello Bob Smith</p>"));
    }

    [Test]
    public void ToStringWorks()
    {
        var template = new Name(firstName: "Bobby", lastName: "Smith");

        Assert.That((string)template, Is.EqualTo("<p>Hello Bobby Smith</p>\n"));
    }

    [Test]
    public void IgnoresNewlines()
    {
        var template = new Paragraph("Test");

        Assert.That(template.Render().Trim(), Is.EqualTo("<p>\n  Test\n</p>"));
    }
    [Test]
    public void BrokenHasNoArgs()
    {

        Assert.That(Broken.Arguments, Is.Empty);
    }
}
