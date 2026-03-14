namespace Strongbars.Benchmarks.Scenarios;

/// <summary>
/// Represents a single templating scenario (one template + one data set).
/// Each implementation is auto-discovered via reflection and run against
/// every engine in <see cref="AllTemplatesBenchmark"/>.
/// </summary>
public interface ITemplateScenario
{
    string Name { get; }

    /// <summary>Called once before benchmarking to pre-compile competitor templates.</summary>
    void Setup();

    string RenderStrongbars();
    string RenderScriban();
    string RenderFluid();
    string RenderHandlebars();
    string RenderStubble();
}
