using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Strongbars.Benchmarks.Scenarios;

namespace Strongbars.Benchmarks;

/// <summary>
/// Benchmarks every <see cref="ITemplateScenario"/> found in this assembly against
/// all five engines.  New scenarios are picked up automatically via reflection –
/// just add a class that implements <see cref="ITemplateScenario"/>.
/// </summary>
[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
[MarkdownExporterAttribute.GitHub]
public class AllTemplatesBenchmark
{
    // Populated by BenchmarkDotNet via ParamsSource
    [ParamsSource(nameof(ScenarioNames))]
    public string ScenarioName { get; set; } = "";

    /// <summary>
    /// Scenario names discovered at startup through reflection.
    /// BenchmarkDotNet iterates this to create one job per scenario.
    /// </summary>
    public static IEnumerable<string> ScenarioNames => TemplateDiscovery.ScenarioNames;

    private ITemplateScenario _scenario = null!;

    [GlobalSetup]
    public void Setup()
    {
        _scenario = TemplateDiscovery.All[ScenarioName];
        _scenario.Setup();
    }

    [Benchmark(Baseline = true, Description = "Strongbars")]
    public string Strongbars() => _scenario.RenderStrongbars();

    [Benchmark(Description = "Scriban")]
    public string Scriban() => _scenario.RenderScriban();

    [Benchmark(Description = "Fluid (Liquid)")]
    public string Fluid() => _scenario.RenderFluid();

    [Benchmark(Description = "Handlebars.Net")]
    public string Handlebars() => _scenario.RenderHandlebars();

    [Benchmark(Description = "Stubble (Mustache)")]
    public string Stubble() => _scenario.RenderStubble();
}
